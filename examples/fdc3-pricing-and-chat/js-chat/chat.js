/*
 * Morgan Stanley makes this available to you under the Apache License,
 * Version 2.0 (the "License"). You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0.
 *
 * See the NOTICE file distributed with this work for additional information
 * regarding copyright ownership. Unless required by applicable law or agreed
 * to in writing, software distributed under the License is distributed on an
 * "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express
 * or implied. See the License for the specific language governing permissions
 * and limitations under the License.
 */

import "bootstrap/dist/css/bootstrap.css";

window.addEventListener('fdc3Ready', async function () {
    intentListener = await window.fdc3.addIntentListener("StartChat", window.app.handleChatIntent);
    await window.fdc3.joinUserChannel("fdc3.channel.1");
});

window.app = function () {
    let channel;
    let contextListener;

    async function handleChatIntent(context, contextMetada) {
        // This is assuming the example runs only a single "Pricing" instance.
        if (channel) {
            return channel;
        }

        channel = await window.fdc3.createPrivateChannel();

        // Add diagnostic messages for testing
        channel.onDisconnect(() => addChatMessage("FDC3: Remote channel disconnected", true));
        channel.onUnsubscribe((ctx) => addChatMessage("FDC3: Remote context listener unsubscribed. Type: " + ctx, true));
        channel.onAddContextListener((ctx) => addChatMessage("FDC3: Remote context listener added. Type: " + ctx, true));

        await addListener();
        return channel;
    };

    function addChatMessage(text, diagnostic) {
        var message = document.createElement("span");

        if (diagnostic) {
            message.classList.add("text-bg-secondary");
        }
        else {
            message.classList.add("text-bg-primary");
        }
        message.classList.add("badge");
        message.classList.add("p3");
        message.textContent = text;

        var div = document.createElement("div");
        div.appendChild(message);

        var chatDiv = document.querySelector("#chat");
        chatDiv.appendChild(div);
    };

    function disconnect() {
        channel.disconnect();
        channel = null;
    }

    function removeListener() {
        contextListener.unsubscribe();
        contextListener = null;
    }

    async function addListener() {
        contextListener = await channel.addContextListener("fdc3.chat.initSettings",
            function (context, _) {
                addChatMessage(context.message.text["text/plain"]);
            }
        );

    }

    async function broadcast() {
        if (!channel) {
            return;
        }
        channel.broadcast({ type: "test" })
    }

    return {
        handleChatIntent: handleChatIntent,
        addChatMessage: addChatMessage,
        disconnect: disconnect,
        removeListener: removeListener,
        addListener: addListener,
        broadcast: broadcast
    }
}();