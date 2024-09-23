/* 
 *  Morgan Stanley makes this available to you under the Apache License,
 *  Version 2.0 (the "License"). You may obtain a copy of the License at
 *       http://www.apache.org/licenses/LICENSE-2.0.
 *  See the NOTICE file distributed with this work for additional information
 *  regarding copyright ownership. Unless required by applicable law or agreed
 *  to in writing, software distributed under the License is distributed on an
 *  "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express
 *  or implied. See the License for the specific language governing permissions
 *  and limitations under the License.
 */

import { AppIdentifier, Channel, Context, OpenError } from "@finos/fdc3";
import { MessageRouter } from "@morgan-stanley/composeui-messaging-client";
import { ComposeUIErrors } from "./ComposeUIErrors";
import { ComposeUITopic } from "./ComposeUITopic";
import { Fdc3OpenRequest } from "./messages/Fdc3OpenRequest";
import { Fdc3OpenResponse } from "./messages/Fdc3OpenResponse";
import { OpenClient } from "./OpenClient";

export class MessageRouterOpenClient implements OpenClient{
    private channel?: Channel | null;

    constructor(
        private readonly instanceId: string,
        private readonly messageRouterClient: MessageRouter) {}

    public async open(app?: string | AppIdentifier | undefined, context?: Context | undefined): Promise<AppIdentifier> {
        if (!app) {
            throw new Error(OpenError.AppNotFound);
        }

        let appIdentifier: AppIdentifier;
        if (typeof app === "string") {
            appIdentifier = { appId: app };
        } else {
            appIdentifier = app;
        }

        if (context && !('type' in context)) {
            throw new Error(OpenError.MalformedContext);
        }

        this.channel = await window.fdc3.getCurrentChannel();

        //TODO:proper context handling
        const request = new Fdc3OpenRequest(
            this.instanceId,
            appIdentifier,
            JSON.stringify(context),
            this.channel?.id);

        const result = await this.messageRouterClient.invoke(ComposeUITopic.open(), JSON.stringify(request));
        if (!result) {
            throw new Error(ComposeUIErrors.NoAnswerWasProvided);
        }

        const response = <Fdc3OpenResponse>JSON.parse(result);
        if (response?.error) {
            throw new Error(response.error);
        }

        if (!response || !response.appIdentifier) {
            throw new Error(ComposeUIErrors.NoAnswerWasProvided);
        }

        return response.appIdentifier;
    }
}