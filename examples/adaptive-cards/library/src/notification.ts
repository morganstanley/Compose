// Morgan Stanley makes this available to you under the Apache License, Version 2.0 (the "License"). You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0. See the NOTICE file distributed with this work for additional information regarding copyright ownership. Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions and limitations under the License.
import * as AdaptiveCards from "adaptivecards";
import { defaultCard, defaultCardTemplate } from "./defaultTemplates";
import markdownit from "markdown-it";
import { CustomTemplate } from "./customElement";
import { hostConfig, Icons, Types, checkType, getIcon } from "./constants";
import defaultCardStyles from "./styles/defaultStyles.css";
import defaultTemplateStyles from "./styles/defaultTemplateStyles.css";

const md = markdownit("commonmark");
AdaptiveCards.AdaptiveCard.onProcessMarkdown = function (text, result) {
  result.outputHtml = md.render(text);
  result.didProcess = true;
};

interface notification {
  count: number;
  card: AdaptiveCards.AdaptiveCard;
}

export class ToastNotification {
  template?: string;
  type: Types;
  icon: string | Icons = Icons.Info;
  card;
  defaultTimer: number = 6000;
  adaptiveTemplate: object;
  static notifications: notification[] = [];
  count: number = ToastNotification.notifications.length;

  constructor(
    type: string = "info",
    template: string,
    icon?: string,
    message?: string
  ) {
    this.type = checkType(type);
    if (icon) {
      this.icon = icon;
    } else {
      this.icon = getIcon(this.type);
    }

    if (template) {
      this.template = template;
      // Create a custom registry for elements
      let elementRegistry =
        new AdaptiveCards.CardObjectRegistry<AdaptiveCards.CardElement>();

      // Populate it with the default set of elements
      AdaptiveCards.GlobalRegistry.populateWithDefaultElements(elementRegistry);

      // Register the custom ProgressBar element
      elementRegistry.register(CustomTemplate.JsonTypeName, CustomTemplate);

      // Parse a card payload using the custom registry
      let serializationContext = new AdaptiveCards.SerializationContext();
      serializationContext.setElementRegistry(elementRegistry);
      this.card = new AdaptiveCards.AdaptiveCard();

      this.adaptiveTemplate = defaultCardTemplate({
        title: `${this.type} Notification`,
        icon: this.icon,
        template: template,
      });

      this.addStyles();

      this.card.parse(this.adaptiveTemplate, serializationContext);
    } else {
      this.adaptiveTemplate = defaultCard({
        title: `${this.type} Notification`,
        icon: this.icon,
        message: message
          ? message
          : "This is a placeholder for the notification message.",
      });
      this.card = new AdaptiveCards.AdaptiveCard();

      this.addStyles();

      this.card.parse(this.adaptiveTemplate);
    }

    ToastNotification.notifications.push({
      count: this.count,
      card: this.card,
    });
  }

  addStyles() {
    this.card.hostConfig = new AdaptiveCards.HostConfig(
      hostConfig(this.typebgColor())
    );
  }

  typebgColor() {
    switch (this.type) {
      case Types.Success:
        return "rgb(201 245 212 / 0.3)";
      case Types.Err:
        return "rgb(237 140 140 / 0.3)";
      default:
        return "rgb(165 176 250 / 0.3)";
    }
  }

  getCard() {
    return this.card;
  }

  get AdaptiveTemplate(): object {
    return this.adaptiveTemplate;
  }

  set AdaptiveTemplate(acTemplate: object) {
    this.adaptiveTemplate = { ...acTemplate };
  }

  renderCard() {
    let newTab: WindowProxy;
    let result: HTMLElement | undefined;

    if (!window.opener) {
      let otherTab = window.open(document.URL, "_blank", "popup");

      if (otherTab) {
        newTab = otherTab;
        otherTab.name = `${this.count}`;
      }
    }

    newTab!.addEventListener("load", (event) => {
      const element = newTab.document.getElementById("container");
      if (element !== null) {
        while (element!.firstChild) {
          element.removeChild(element.firstChild);
        }
      }

      result = this.card.render(newTab.document.body);
      newTab.setTimeout(() => {
        if (result) {
          result.remove();
          newTab.close();
        }
      }, this.defaultTimer);

      this.renderStyles(newTab!.document);
    });

    this.card.onExecuteAction = function (action) {
      newTab.close();
    };
  }

  renderStyles(doc: Document) {
    const style = document.createElement("style");

    if (this.template) {
      style.innerText = defaultTemplateStyles;
    } else {
      style.innerText = defaultCardStyles;
    }

    doc.head.appendChild(style);
  }
}
