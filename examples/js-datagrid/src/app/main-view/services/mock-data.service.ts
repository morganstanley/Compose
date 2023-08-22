/* Morgan Stanley makes this available to you under the Apache License, Version 2.0 (the "License"). You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0. See the NOTICE file distributed with this work for additional information regarding copyright ownership. Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions and limitations under the License. */

import { Injectable } from '@angular/core';
import { interval, Subject } from 'rxjs';
import { Market } from './mock-data';
import { Channel, Context } from '@finos/fdc3';

@Injectable({
  providedIn: 'root'
})
export class MockDataService{
  public subject: Subject<any> = new Subject<any>();
  public marketData: any;
  private market: Market;
  private currentChannel: Channel | null;
  private connected: Boolean = false;
  private connecting: Promise<void>;

  constructor(){
    this.market = new Market();
    this.connecting = new Promise(async(resolve, reject) => {
      try{
        resolve(await this.checkFdc3Connection());
      } catch(err) {
        reject(err);
      }
    });

    interval(1000).subscribe(() => {
      this.marketData = this.market.generateNewMarketNumbers();
      this.subject.next(this.marketData);
    });
  }

  private async checkFdc3Connection(): Promise<void> {
    if(!this.connected) {
      this.currentChannel = await window.fdc3.getCurrentChannel();
      if (!this.currentChannel) {
        await window.fdc3.joinUserChannel("default");
      }
      this.connected = true;
    }
  }

  public async publishSymbolData(symbol: any|undefined): Promise<void> {
    if(symbol){
      await this.connecting;
      //TODO: involve DataService to generate the data for the chart
      let marketSymbol = this.market.createMarketSymbol(symbol);

      const context: Context = {
        type: 'fdc3.instrument',
        id: {
          ticker: marketSymbol?.symbol,
          buyData: marketSymbol?.buy,
          sellData: marketSymbol?.sell
        }
      }

      await window.fdc3.broadcast(context);
    }
  }
}
