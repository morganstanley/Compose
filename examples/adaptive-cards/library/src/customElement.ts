// Morgan Stanley makes this available to you under the Apache License, Version 2.0 (the "License"). You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0. See the NOTICE file distributed with this work for additional information regarding copyright ownership. Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions and limitations under the License.
import * as AdaptiveCards from "adaptivecards";
import sanitizeHtml from "sanitize-html";

export class CustomTemplate extends AdaptiveCards.CardElement {
  private parser = new DOMParser();
  static readonly JsonTypeName = "AdaptiveHTML";

  //#region Schema
  static readonly templateStringProperty = new AdaptiveCards.StringProperty(
    AdaptiveCards.Versions.v1_0,
    "templateString"
  );

  @AdaptiveCards.property(CustomTemplate.idProperty)
  get templateString(): string {
    return this.getValue(CustomTemplate.templateStringProperty);
  }

  set templateString(value: string) {
    let santitizedString = sanitizeHtml(value);

    if (this.templateString !== santitizedString) {
      this.setValue(CustomTemplate.templateStringProperty, santitizedString);

      this.updateLayout();
    }
  }

  //#endregion

  private _templateElement?: HTMLElement;

  protected internalRender(): HTMLElement {
    let element = document.createElement("div");

    this._templateElement = document.createElement("div");

    this._templateElement.insertAdjacentHTML("afterbegin", this.templateString);

    element.append(this._templateElement);

    return element;
  }

  getJsonTypeName(): string {
    return CustomTemplate.JsonTypeName;
  }

  updateLayout(processChildren: boolean = true) {
    super.updateLayout(processChildren);

    this._templateElement = document.createElement("div");

    this._templateElement.insertAdjacentHTML("afterbegin", this.templateString);
  }
}
