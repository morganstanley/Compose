<!--- Morgan Stanley makes this available to you under the Apache License, Version 2.0 (the "License"). You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0. See the NOTICE file distributed with this work for additional information regarding copyright ownership. Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions and limitations under the License.
--->

# ComposeUI Notification Library

This is a library that allows users to create notifications using adaptive cards within the browser. Each adaptive card is rendered in a new browser window and is displayed for 6 seconds after which the newly created browser window is closed. There are three types of notifications that a user can create: 
- info
- error 
- success

Each of these types of notifications have a default format and styles that are associated with them. 

Another feature of this library is the ability to render an html template that can be passed by the user which is rendered as the body of the notification. 

### Library Inputs:
- type: optional - accepted values: ['info','error','success'], default: 'info'
- template: optional - acepted values: string containing html to be rendered
- icon: optional - accepted values: uri for an icon to display in the toast notification
- message: optional - accepted values: string. This can be used to display a placeholder message in the default notification when a template is not passed.

### Development flow
1. Run npm install in the compose-ui folder.
` npm i `

2. These are commands you would need to use in order to build this library, it's subsequest web application that would showcase an example of the library and run that application.
``` 
npx lerna run build --stream --scope=@morgan-stanley/adaptive-card-notification

npx lerna run build --stream --scope=@morgan-stanley/composeui-example-adaptive-cards

npx lerna run start --stream --scope=@morgan-stanley/composeui-example-adaptive-cards
```