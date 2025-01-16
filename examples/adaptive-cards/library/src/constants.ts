// Morgan Stanley makes this available to you under the Apache License, Version 2.0 (the "License"). You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0. See the NOTICE file distributed with this work for additional information regarding copyright ownership. Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions and limitations under the License.
import error from "./img/error.png";
import info from "./img/info.png";
import success from "./img/sucess.png";

export const hostConfig = (bgColor: string) => {
  return {
    fontFamily: "Segoe UI, Helvetica Neue, sans-serif",
    containerStyles: {
      default: {
        backgroundColor: `${bgColor}`,
      },
    },
  };
};

export const images = [error, info, success];

export enum Icons {
  Info = info,
  Err = error,
  Success = success,
}

export enum Types {
  Info = "Information",
  Err = "Error",
  Success = "Success",
}

export function checkType(type: string) {
  switch (type) {
    case "error":
      return Types.Err;
    case "success":
      return Types.Success;
    default:
      return Types.Info;
  }
}

export function getIcon(type: Types) {
  switch (type) {
    case Types.Err:
      return Icons.Err;
    case Types.Success:
      return Icons.Success;
    case Types.Info:
      return Icons.Info;
  }
}

export interface Window { adaptiveToast: any; }