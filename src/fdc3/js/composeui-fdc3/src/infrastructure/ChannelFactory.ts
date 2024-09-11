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

import { Channel, ContextHandler, IntentHandler, Listener, PrivateChannel } from "@finos/fdc3";
import { ChannelType } from "./ChannelType";

export interface ChannelFactory {
    getChannel(channelId: string, channelType: ChannelType): Promise<Channel>;
    createPrivateChannel(): Promise<PrivateChannel>;
    createAppChannel(channelId: string): Promise<Channel>;
    joinUserChannel(channelId: string): Promise<Channel>;
    getUserChannels(): Promise<Channel[]>;
    getIntentListener(intent: string, handler: IntentHandler): Promise<Listener>;
    getContextListener(channel?: Channel, handler?: ContextHandler, contextType?: string | null): Promise<Listener>;
}