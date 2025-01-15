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
 *  
 */

import { Context, ContextHandler, Listener, ResultError } from "@finos/fdc3";
import { MessageRouter, TopicMessage } from "@morgan-stanley/composeui-messaging-client";
import { ChannelType } from "./ChannelType";
import { Unsubscribable } from "rxjs";
import { ComposeUITopic } from "./ComposeUITopic";
import { Fdc3RemoveContextListenerRequest } from "./messages/Fdc3RemoveContextListenerRequest";
import { Fdc3RemoveContextListenerResponse } from "./messages/Fdc3RemoveContextListenerResponse";
import { ComposeUIErrors } from "./ComposeUIErrors";
import { Fdc3AddContextListenerRequest } from "./messages/Fdc3AddContextListenerRequest";
import { Fdc3AddContextListenerResponse } from "./messages/Fdc3AddContextListenerResponse";

export class ComposeUIContextListener implements Listener {
    private readonly messageRouterClient: MessageRouter;
    private unsubscribable?: Unsubscribable;
    private readonly handler: ContextHandler;
    public readonly contextType?: string;
    private isSubscribed: boolean = false;
    private id?: string;
    private unsubscribeCallback?: (x: ComposeUIContextListener) => void;
    private openHandled: boolean;
    private contexts: Context[] = [];

    constructor(openHandled: boolean, messageRouterClient: MessageRouter, handler: ContextHandler, contextType?: string) {
        this.openHandled = openHandled;
        this.messageRouterClient = messageRouterClient;
        this.handler = handler;
        this.contextType = contextType;
    }

    public async subscribe(channelId: string, channelType: ChannelType): Promise<void> {
        await this.registerContextListener(channelId, channelType);
        const subscribeTopic = ComposeUITopic.broadcast(channelId, channelType);
        this.unsubscribable = await this.messageRouterClient.subscribe(subscribeTopic, (topicMessage: TopicMessage) => {

            if (topicMessage.context.sourceId == this.messageRouterClient.clientId) {
                return;
            }

            //TODO: integration test
            const context = <Context>JSON.parse(topicMessage.payload!);
            if (!this.contextType || this.contextType == context!.type) {
                if (this.openHandled === true) {
                    this.handler!(context!);
                } else {
                    this.contexts.push(context);
                }
            }
        });
        this.isSubscribed = true;
    }

    public async handleContextMessage(context: Context): Promise<void> {
        if (!this.isSubscribed) {
            throw new Error("The current listener is not subscribed.");
        }

        if (this.contextType && this.contextType != null && this.contextType != context.type) {
            throw new Error(`The current listener is not able to handle context type ${context.type}. It is registered to handle ${this.contextType}.`)
        }
        
        //If the opened app did not resolved the context that was received by the fdc3.open call, we cache the item.
        if (this.openHandled !== true) {
            this.contexts.push(context);
            return;
        }

        this.handler(context);
    }

    public setUnsubscribeCallback(unsubscribeCallback: (x: ComposeUIContextListener) => void): void {
        this.unsubscribeCallback = unsubscribeCallback;
    }

    public setOpenHandled(openHandled: boolean): void {
        this.openHandled = openHandled;

        if (this.openHandled === true) {
            this.contexts.forEach(context => {
                this.handler(context)
            });

            this.contexts = [];
        }
    }

    public async unsubscribe(): Promise<void> {
        if (!this.unsubscribable || !this.isSubscribed) {
            return;
        }
        
        try {
            await this.leaveChannel();
        } catch(err) {
            console.log(err);
        }

        this.unsubscribable.unsubscribe();
        this.isSubscribed = false;

        if (this.unsubscribeCallback) {
            this.unsubscribeCallback(this);
        }
    }

    private async registerContextListener(channelId: string, channelType: ChannelType) :Promise<void>{
        const request = new Fdc3AddContextListenerRequest(window.composeui.fdc3.config?.instanceId!, this.contextType, channelId, channelType);
        const response = await this.messageRouterClient.invoke(ComposeUITopic.addContextListener(), JSON.stringify(request));

        if (!response) {
            throw new Error(ComposeUIErrors.NoAnswerWasProvided);
        }

        const result = <Fdc3AddContextListenerResponse>JSON.parse(response);
        if (result.error) {
            throw new Error(result.error);
        } else if (!result.success) {
            throw new Error(ComposeUIErrors.SubscribeFailure);
        }

        this.id = result.id!
    }

    private async leaveChannel() : Promise<void> {
        const request = new Fdc3RemoveContextListenerRequest(window.composeui.fdc3.config?.instanceId!, this.id!, this.contextType);
        const result = await this.messageRouterClient.invoke(ComposeUITopic.removeContextListener(), JSON.stringify(request));
        if (!result) {
            throw new Error(ComposeUIErrors.NoAnswerWasProvided);
        }

        const response = <Fdc3RemoveContextListenerResponse>JSON.parse(result);
        if (response.error) {
            throw new Error(response.error);
        }

        if (!response.success) {
            throw new Error(ComposeUIErrors.UnsubscribeFailure);
        }
        
        return;
    }
}
