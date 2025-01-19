// Morgan Stanley makes this available to you under the Apache License, Version 2.0 (the "License"). You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0. See the NOTICE file distributed with this work for additional information regarding copyright ownership. Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions and limitations under the License.
import cancel from './img/cancel.png';

export function defaultCard (context:any) {
	const data = { ...context };
	return {
		"type": "AdaptiveCard",
		"version": "1.6",
		"body": [
			{
				"type": "ColumnSet",
				"columns": [
					{
						"type": "Column",
						"items": [
							{
								"type": "Image",
								"style": "Person",
								"url": `${data.icon}`,
								"size": "Small"
							}
						],
						"width": "auto"
					},
					{
						"type": "Column",
						"items": [
							{
								"type": "TextBlock",
								"weight": "Bolder",
								"text": `${data.title}`,
								"wrap": true
							},
							{
								"type": "TextBlock",
								  "text": `${data.message}`,
								  "wrap": true
							}
						],
						"width": "stretch"
					}
				]
			}
		],
		"actions": [
			{
				type: "Action.Submit",
                iconUrl: cancel,
                id: "closeButton",     
			}
		]
	}
} 

export function defaultCardTemplate(context:any) {
	const data = { ...context };

return {
    type: "AdaptiveCard",
    version: "1.6",
    body: [
        {
            "type": "ColumnSet",
            "columns": [
                {
                    "type": "Column",
					"items": [
						{
							"type": "Image",
							"style": "Person",
							"url": `${data.icon}`,
							"size": "Small"
						}
					],
					"width": "auto"
                },
                {
					"type": "Column",
					"items": [
						{
							"type": "TextBlock",
							"weight": "Bolder",
							"text": `${data.title}`,
							"wrap": true
						}
					],
					"width": "stretch"
				}
            ]
        },
		{
			type: "AdaptiveHTML",
        	templateString: `${data.template}`
		}
    ],
    actions: [
		{
            type: "Action.OpenUrl",
            title: "Ok"      
        },
        {
            type: "Action.OpenUrl",
            title: "Cancel"      
        }
    ]
}
}