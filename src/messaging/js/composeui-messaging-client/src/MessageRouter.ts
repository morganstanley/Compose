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

import { Unsubscribable } from "rxjs";
import { PublishOptions } from "./PublishOptions";
import { EndpointDescriptor } from "./EndpointDescriptor";
import { InvokeOptions } from "./InvokeOptions";
import { MessageBuffer } from "./MessageBuffer";
import { MessageHandler } from "./MessageHandler";
import { TopicMessage } from "./TopicMessage";
import { TopicSubscriber } from "./TopicSubscriber";
import { WebSocketConnection, WebSocketOptions } from "./client/websocket";
import { MessageRouterClient, MessageRouterOptions } from "./client";

export interface MessageRouter {
    clientId?: string;
    connect(): Promise<void>;
    subscribe(topic: string, subscriber: TopicSubscriber | ((message: TopicMessage) => void)): Promise<Unsubscribable>;
    publish(topic: string, payload?: MessageBuffer, options?: PublishOptions): Promise<void>;
    invoke(endpoint: string, payload?: MessageBuffer, options?: InvokeOptions): Promise<MessageBuffer | undefined>;
    registerService(endpoint: string, handler: MessageHandler, descriptor?: EndpointDescriptor): Promise<void>;
    unregisterService(endpoint: string): Promise<void>;
    registerEndpoint(endpoint: string, handler: MessageHandler, descriptor?: EndpointDescriptor): Promise<void>;
    unregisterEndpoint(endpoint: string): Promise<void>;
}

export type MessageRouterConfig = MessageRouterOptions & {
    webSocket?: WebSocketOptions
};

declare global {
    var composeui: {
        messageRouterConfig?: MessageRouterConfig
    }
}

export function createMessageRouter(config?: MessageRouterConfig): MessageRouter {
    
    config ??= window.composeui?.messageRouterConfig;

    if (config?.webSocket) {
        const connection = new WebSocketConnection(config.webSocket);
        
        return new MessageRouterClient(connection, config);
    }

    throw ConfigNotFound();

    function ConfigNotFound() {
        return new Error("Unable to create the MessageRouter client, configuration is missing.");
    }
}
