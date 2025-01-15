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

import {
    AppIdentifier,
    AppIntent,
    AppMetadata,
    Channel,
    ChannelError,
    Context,
    ContextHandler,
    DesktopAgent,
    ImplementationMetadata,
    IntentHandler,
    IntentResolution,
    Listener,
    PrivateChannel
} from '@finos/fdc3';

import { MessageRouter } from '@morgan-stanley/composeui-messaging-client';
import { ComposeUIContextListener } from './infrastructure/ComposeUIContextListener';
import { ComposeUIErrors } from './infrastructure/ComposeUIErrors';
import { ChannelFactory } from './infrastructure/ChannelFactory';
import { MessageRouterChannelFactory } from './infrastructure/MessageRouterChannelFactory';
import { MessageRouterIntentsClient } from './infrastructure/MessageRouterIntentsClient';
import { IntentsClient } from './infrastructure/IntentsClient';
import { MetadataClient } from './infrastructure/MetadataClient';
import { MessageRouterMetadataClient } from './infrastructure/MessageRouterMetadataClient';
import { OpenClient } from "./infrastructure/OpenClient";
import { MessageRouterOpenClient } from "./infrastructure/MessageRouterOpenClient";

export class ComposeUIDesktopAgent implements DesktopAgent {
    private appChannels: Channel[] = [];
    private userChannels: Channel[] = [];
    private privateChannels: Channel[] = [];
    private currentChannel?: Channel;
    private topLevelContextListeners: ComposeUIContextListener[] = [];
    private intentListeners: Listener[] = [];
    private channelFactory: ChannelFactory;
    private intentsClient: IntentsClient;
    private metadataClient: MetadataClient;
    private openClient: OpenClient;
    private openedAppContext?: Context;
    private openedAppContextHandled: boolean = false;

    //TODO: we should enable passing multiple channelId to the ctor.
    constructor(messageRouterClient: MessageRouter, channelFactory?: ChannelFactory) {
        if (!window.composeui.fdc3.config || !window.composeui.fdc3.config.instanceId) {
            throw new Error(ComposeUIErrors.InstanceIdNotFound);
        }

        // TODO: inject this directly instead of the messageRouter
        this.channelFactory = channelFactory ?? new MessageRouterChannelFactory(messageRouterClient, window.composeui.fdc3.config.instanceId);
        this.intentsClient = new MessageRouterIntentsClient(messageRouterClient, this.channelFactory);
        this.metadataClient = new MessageRouterMetadataClient(messageRouterClient, window.composeui.fdc3.config);
        this.openClient = new MessageRouterOpenClient(window.composeui.fdc3.config.instanceId!, messageRouterClient, window.composeui.fdc3.openAppIdentifier);
    }

    public async open(app?: string | AppIdentifier, context?: Context): Promise<AppIdentifier> {
        return await this.openClient.open(app, context);
    }

    public async findIntent(intent: string, context?: Context, resultType?: string): Promise<AppIntent> {
        return await this.intentsClient.findIntent(intent, context, resultType);
    }

    public async findIntentsByContext(context: Context, resultType?: string): Promise<Array<AppIntent>> {
        return await this.intentsClient.findIntentsByContext(context, resultType);
    }

    public async findInstances(app: AppIdentifier): Promise<Array<AppIdentifier>> {
        return await this.metadataClient.findInstances(app);
    }

    public async broadcast(context: Context): Promise<void> {
        if (!this.currentChannel) {
            throw new Error(ComposeUIErrors.CurrentChannelNotSet);
        }

        return this.currentChannel.broadcast(context);
    }

    public async raiseIntent(intent: string, context: Context, app?: string | AppIdentifier): Promise<IntentResolution> {
        return await this.intentsClient.raiseIntent(intent, context, app);
    }

    //TODO
    public async raiseIntentForContext(context: Context, app?: string | AppIdentifier): Promise<IntentResolution> {
        return await this.intentsClient.raiseIntentForContext(context, app);
    }

    public async addIntentListener(intent: string, handler: IntentHandler): Promise<Listener> {
        var listener = await this.channelFactory.getIntentListener(intent, handler);
        this.intentListeners.push(listener);
        return listener;
    }

    public async addContextListener(contextType?: string | null | ContextHandler, handler?: ContextHandler): Promise<Listener> {
        if (contextType && typeof contextType != 'string') {
            handler = contextType;
            contextType = null;
        }

        if (this.openedAppContext 
            && handler 
            && (contextType == this.openedAppContext?.type || this.openedAppContext.type == null || !this.openedAppContext.type)) {                
                
                if (this.openedAppContextHandled === false) {
                    handler(this.openedAppContext);
                    this.openedAppContextHandled = true;
                }
        } 
        
        if (!this.openedAppContext && this.openedAppContextHandled !== true) {
            //There is no context to handle -aka app was not opened via the fdc3.open
            this.openedAppContextHandled = true;
        }

        const listener = <ComposeUIContextListener>await this.channelFactory.getContextListener(this.openedAppContextHandled, this.currentChannel, handler, contextType);
        this.topLevelContextListeners.push(listener);

        if (!this.currentChannel) {
            return listener;
        }

        return await new Promise<Listener>((resolve) => {
            resolve(listener);
        }).finally(() => {
            this.getLastContext(listener);
        });
    }

    public async getUserChannels(): Promise<Array<Channel>> {
        return await this.channelFactory.getUserChannels();
    }

    public async joinUserChannel(channelId: string): Promise<void> {
        if (this.currentChannel) {
            //DesktopAgnet clients can listen on only one channel
            await this.leaveCurrentChannel();
        }

        let channel = this.userChannels.find(innerChannel => innerChannel.id == channelId);
        if (!channel) {
            channel = await this.channelFactory.joinUserChannel(channelId);
            
            if (!channel) {
                throw new Error(ChannelError.NoChannelFound);
            }
    
            this.addChannel(channel);
        }

        this.currentChannel = channel;

        for (const listener of this.topLevelContextListeners) {
            await listener.subscribe(this.currentChannel.id, this.currentChannel.type)
                .finally(() => {
                    this.getLastContext(listener);
                });
        }
    }

    public async getOrCreateChannel(channelId: string): Promise<Channel> {
        let appChannel = this.appChannels.find(channel => channel.id == channelId);
        if (appChannel) {
            return appChannel;
        }

        appChannel = await this.channelFactory.createAppChannel(channelId);

        this.addChannel(appChannel!);
        return appChannel!;
    }

    public async createPrivateChannel(): Promise<PrivateChannel> {
        return await this.channelFactory.createPrivateChannel();
    }

    public async getCurrentChannel(): Promise<Channel | null> {
        return this.currentChannel ?? null;
    }

    public async leaveCurrentChannel(): Promise<void> {
        //The context listeners, that have been added through the `fdc3.addContextListener()` should unsubscribe
        for (const listener of this.topLevelContextListeners) {
            await listener.unsubscribe();
        }
        
        this.currentChannel = undefined;
    }

    public async getInfo(): Promise<ImplementationMetadata> {
        return await this.metadataClient.getInfo();
    }

    public async getAppMetadata(app: AppIdentifier): Promise<AppMetadata> {
        return await this.metadataClient.getAppMetadata(app);
    }

    // Deprecated, alias to getUserChannels
    // https://fdc3.finos.org/docs/2.0/api/ref/DesktopAgent#getsystemchannels-deprecated
    public getSystemChannels(): Promise<Channel[]> {
        return this.getUserChannels();
    }

    // Deprecated, alias to joinUserChannel
    // https://fdc3.finos.org/docs/2.0/api/ref/DesktopAgent#joinchannel-deprecated
    public joinChannel(channelId: string): Promise<void> {
        return this.joinUserChannel(channelId);
    }

    public async getOpenedAppContext(): Promise<void> {
        try {
            this.openedAppContext = await this.openClient.getOpenedAppContext();
        } catch (err) {
            console.error("The opened app via fdc3.open() could not retrieve the context: ", err);
        }
    }

    private addChannel(channel: Channel): void {
        if (channel == null) return;
        switch (channel.type) {
            case "app":
                this.appChannels.push(channel);
                break;
            case "user":
                this.userChannels.push(channel);
                break;
            case "private":
                this.privateChannels.push(channel);
                break;
        }
    }

    private async getLastContext(listener: ComposeUIContextListener) : Promise<void> {
        const lastContext = await this.currentChannel!.getCurrentContext(listener.contextType);

        if (lastContext) {
            await listener.handleContextMessage(lastContext);
        }
    }
}