/* Morgan Stanley makes this available to you under the Apache License, Version 2.0 (the "License"). You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0. See the NOTICE file distributed with this work for additional information regarding copyright ownership. Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions and limitations under the License. */
import { Component, NgZone, OnDestroy, OnInit } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';
import { MatTableDataSource, MatTableModule } from '@angular/material/table';
import { Channel, Listener } from '@finos/fdc3';
import {MatTreeFlatDataSource, MatTreeFlattener, MatTreeModule} from '@angular/material/tree';
import { FlatTreeControl } from '@angular/cdk/tree';
import { MatButtonModule } from '@angular/material/button';
import { Subject } from 'rxjs';

export interface Symbol {
  Description: string;
  Symbol: string;
  Children?: SymbolElement[];
}

export interface SymbolElement extends Symbol{
  AskPrice?: number;
  AskSize?: number;
  BidPrice?: number;
  BidSize?: number;
  LastTrade?: Date;
}

let ELEMENT_DATA : Symbol[] = [
  { 
    Description: "Apple Inc", 
    Symbol:"AAPL", 
    Children: [
      { AskPrice: 300.528, AskSize:50, BidPrice:298, BidSize:1, Description: "Apple Inc", Symbol:"AAPL"  },
      { AskPrice: 310.528, AskSize:50, BidPrice:298, BidSize:1, Description: "Apple Inc", Symbol:"AAPL"  },
      { AskPrice: 320.528, AskSize:50, BidPrice:298, BidSize:1, Description: "Apple Inc", Symbol:"AAPL"  },
    ] 
  },
  {
    Description: "Samsung Electronics Co., Ltd.", 
    Symbol:"SSNLF",
    Children: [
      { AskPrice: 151, AskSize:20, BidPrice:151, BidSize:1, Description: "Samsung Electronics Co., Ltd.", Symbol:"SSNLF"  },
      { AskPrice: 181, AskSize:20, BidPrice:151, BidSize:1, Description: "Samsung Electronics Co., Ltd.", Symbol:"SSNLF"  },
      { AskPrice: 251, AskSize:20, BidPrice:151, BidSize:1, Description: "Samsung Electronics Co., Ltd.", Symbol:"SSNLF"  },
    ]
  },
  {
    Description: "Dr. Reddy's Laboritatories Limited", 
    Symbol:"RDY",
    Children: [
      { AskPrice: 150, AskSize:50, BidPrice:150, BidSize:1, Description: "Dr. Reddy's Laboritatories Limited", Symbol:"RDY"  },
      { AskPrice: 250, AskSize:50, BidPrice:150, BidSize:1, Description: "Dr. Reddy's Laboritatories Limited", Symbol:"RDY"  },
      { AskPrice: 270, AskSize:50, BidPrice:150, BidSize:1, Description: "Dr. Reddy's Laboritatories Limited", Symbol:"RDY"  },
    ]
  },
  {
    Description: "AC Immune SA", 
    Symbol:"ACIU",
    Children: [
      { AskPrice: 110, AskSize:110, BidPrice:136, BidSize:1, Description: "AC Immune SA", Symbol:"ACIU"  },
      { AskPrice: 120, AskSize:110, BidPrice:136, BidSize:1, Description: "AC Immune SA", Symbol:"ACIU"  },
    ]
  },
  {
    Description: "NVIDIA Corporation", 
    Symbol:"NVDA",
    Children: [
      { AskPrice: 100.28, AskSize:320, BidPrice:66, BidSize:1, Description: "NVIDIA Corporation", Symbol:"NVDA"  },
      { AskPrice: 150.28, AskSize:32, BidPrice:66, BidSize:1, Description: "NVIDIA Corporation", Symbol:"NVDA"  },
      { AskPrice: 200.28, AskSize:20, BidPrice:66, BidSize:1, Description: "NVIDIA Corporation", Symbol:"NVDA"  },
      { AskPrice: 300.28, AskSize:3, BidPrice:66, BidSize:1, Description: "NVIDIA Corporation", Symbol:"NVDA"  },
    ]
  },
  {
    Description: "Microsoft Corporation", 
    Symbol:"MSFT",
    Children: [
      { AskPrice: 87.10, AskSize:12, BidPrice:95, BidSize:1, Description: "Microsoft Corporation", Symbol:"MSFT"  },
      { AskPrice: 97.10, AskSize:12, BidPrice:95, BidSize:1, Description: "Microsoft Corporation", Symbol:"MSFT"  },
      { AskPrice: 127.10, AskSize:12, BidPrice:95, BidSize:1, Description: "Microsoft Corporation", Symbol:"MSFT"  },
    ]
  },
  {
    Description: "Tesla, Inc.", 
    Symbol:"TSLA",
    Children: [
      { AskPrice: 27.311, AskSize:99, BidPrice:26, BidSize:43, Description: "Tesla, Inc.", Symbol:"TSLA"  },
      { AskPrice: 47.311, AskSize:9, BidPrice:26, BidSize:43, Description: "Tesla, Inc.", Symbol:"TSLA"  },
      { AskPrice: 67.311, AskSize:11, BidPrice:26, BidSize:43, Description: "Tesla, Inc.", Symbol:"TSLA"  },
      { AskPrice: 77.311, AskSize:22, BidPrice:26, BidSize:43, Description: "Tesla, Inc.", Symbol:"TSLA"  },
      { AskPrice: 87.311, AskSize:33, BidPrice:26, BidSize:43, Description: "Tesla, Inc.", Symbol:"TSLA"  },
    ]
  },
  {
    Description: "Serve Robotics Inc.", 
    Symbol:"SERV",
    Children: [
      { AskPrice: 30.98, AskSize:10, BidPrice:30, BidSize:2, Description: "Serve Robotics Inc.", Symbol:"SERV"  },
    ]
  },
  {
    Description: "CrowdStrike Holdings, Inc.", 
    Symbol:"CRWD",
    Children: [
      { AskPrice: 50.7, AskSize:30, BidPrice:52, BidSize:7, Description: "CrowdStrike Holdings, Inc.", Symbol:"CRWD"  },
    ]
  },
  {
    Description: "Bitdeer Technologies Group", 
    Symbol:"BTDR",
    Children: [
      { AskPrice: 126, AskSize:90, BidPrice:165, BidSize:5, Description: "Bitdeer Technologies Group", Symbol:"BTDR"  },
    ]
  },
  {
    Description: "Rubik, Inc.", 
    Symbol:"RBRK",
    Children: [
      { AskPrice: 347, AskSize:330, BidPrice:377, BidSize:25, Description: "Rubik, Inc.", Symbol:"RBRK"  },
    ]
  },
  {
    Description: "Delta Air Lines, Inc.", 
    Symbol:"DAL",
    Children: [
      { AskPrice: 55, AskSize:80, BidPrice:50.25, BidSize:74, Description: "Delta Air Lines, Inc.", Symbol:"DAL"  },
      { AskPrice: 75, AskSize:80, BidPrice:50.25, BidSize:74, Description: "Delta Air Lines, Inc.", Symbol:"DAL"  },
      { AskPrice: 85, AskSize:80, BidPrice:50.25, BidSize:74, Description: "Delta Air Lines, Inc.", Symbol:"DAL"  },
    ]
  },
  {
    Description: "The Walt Disney Company", 
    Symbol:"DIS",
    Children: [
      { AskPrice: 98.77, AskSize:7, BidPrice:157, BidSize:64, Description: "The Walt Disney Company", Symbol:"DIS"  },
      { AskPrice: 158.77, AskSize:7, BidPrice:157, BidSize:64, Description: "The Walt Disney Company", Symbol:"DIS"  },
      { AskPrice: 167.77, AskSize:16, BidPrice:157, BidSize:64, Description: "The Walt Disney Company", Symbol:"DIS"  },
      { AskPrice: 200.77, AskSize:32, BidPrice:157, BidSize:64, Description: "The Walt Disney Company", Symbol:"DIS"  },
    ]
  }
];

export interface SymbolFlatNode {
  Expandable: boolean;
  Symbol: string;
  AskPrice: number | undefined;
  AskSize: number | undefined;
  BidPrice: number | undefined;
  BidSize: number | undefined;
  Description: string;
  LastTrade: Date| undefined;
  Level: number;
}

interface SubjectKeyValuePair {
  Symbol: string | undefined;
  DataSource: Symbol[];
}

@Component({
  selector: 'app-market-watch',
  templateUrl: './market-watch.component.html',
  standalone: true,
  imports: [MatTableModule, MatIconModule, MatButtonModule, MatTreeModule],
  styleUrl: './market-watch.component.scss'
})
export class MarketWatchComponent implements OnInit, OnDestroy{
  private listeners: Listener[] = new Array<Listener>();
  private subject: Subject<SubjectKeyValuePair> = new Subject<SubjectKeyValuePair>();

  private transformer = (node: SymbolElement, level: number) => {
    return {
      Expandable: !!node.Children && node.Children.length > 0,
      Symbol: node.Symbol,
      AskPrice: node.AskPrice,
      AskSize: node.AskSize,
      BidPrice: node.BidPrice,
      BidSize: node.BidSize,
      Description: node.Description,
      LastTrade: node.LastTrade,
      Level: level,
    };
  }

  public treeControl = new FlatTreeControl<SymbolFlatNode>(node => node.Level, node => node.Expandable);

  public treeFlattener = new MatTreeFlattener(
      this.transformer, node => node.Level, 
      node => node.Expandable, node => node.Children);

  dataSource = new MatTreeFlatDataSource(this.treeControl, this.treeFlattener);

  hasChild = (_: number, node: SymbolFlatNode) => node.Expandable;

  constructor(private ngZone: NgZone) {
    this.subject.subscribe(data => {
      this.ngZone.run(() => {
        this.dataSource = new MatTreeFlatDataSource(this.treeControl, this.treeFlattener);
        this.dataSource.data = [...data.DataSource];
        this.treeControl.dataNodes.forEach(x => {
          if (x.Symbol === data.Symbol && x.Expandable) {
            this.treeControl.expand(x);
          }
        });
      });
    })
  }

  async ngOnDestroy() {
    this.listeners.forEach(async listener => {
      await listener.unsubscribe();
    });

    this.listeners = [];
  }

  async ngOnInit() {
    this.subject.next(
      { 
        Symbol: undefined,
        DataSource: [...ELEMENT_DATA] 
      });

    try {
      this.channel = await window.fdc3.getOrCreateChannel('tradeIdeasChannel');
      const listener = await this.channel.addContextListener("fdc3.trade", async(context, metadata) => {
        const data = context['data'];
        let symbols = ELEMENT_DATA.filter(x => x.Symbol == data.symbol && x.Children?.length && x.Children?.length > 0);
        const topic: string = "fdc3." + data.symbol + "." + data.trader;

        if (!symbols || symbols.length == 0) {
          await this.channel!.broadcast(
            {
              type: topic,
              result: {
                success: false,
                action: "BUY",
                error: "No symbol found."
              }
            });

          return;
        }

        if (data.action === 'BUY') {
          const sumQuantity = symbols.reduce((sum, current) => {
            if (current.Children && current.Children.length > 0) {
              let s: number = current.Children.reduce((t, currentSymbol) => {
                if(currentSymbol.AskSize) {
                  return t + currentSymbol.AskSize;
                }
                return t + 0;
              }, 0);

              return sum + s;
            }
            return sum + 0;
          }, 0);

  
          if (sumQuantity < data.quantity) {
            await this.channel!.broadcast(
              {
                type: topic,
                result: {
                  success: false,
                  action: "BUY",
                  error: "Too much ticks were requested; not enough symbols are available on the target."
                }
              });
  
            return;
          }
  
          let price: number = 0;
          let size = data.quantity;
          for (let element of ELEMENT_DATA) {
            if (size == 0) {
              break;
            }

            if (element.Symbol != data.symbol || !element.Children || element.Children.length <= 0) {
              continue;
            }

            if (element.Children.at(0) && element.Children.at(0)?.AskSize && element.Children.at(0)!.AskSize! >= data.quantity) {
              element.Children.at(0)!.AskSize! = element.Children.at(0)!.AskSize! - data.quantity;
              element.Children.at(0)!.LastTrade = data.timestamp;
              price = element.Children.at(0)!.AskPrice != undefined ? element.Children.at(0)!.AskPrice! * data.quantity : 0;
              size = 0;

              await this.channel!.broadcast({
                type: topic,
                result: {
                  success: true,
                  action: "BUY",
                  tradePrice: price
                }
              });

              break;
            }
            
            for (let innerElement of element.Children) {
              if (innerElement.Symbol != data.symbol) {
                continue;
              }

              if (innerElement.AskSize && innerElement.AskSize >= size) {
                price = price + size * (innerElement.AskPrice == undefined ? 0 : innerElement.AskPrice);
                innerElement.AskSize = innerElement.AskSize - size;
                innerElement.LastTrade = data.timestamp;
                size = 0;
                break;
              } else if (innerElement.AskSize && innerElement.AskSize < size && innerElement.AskSize != 0) {
                price = price + innerElement.AskSize * (innerElement.AskPrice == undefined ? 0 : innerElement.AskPrice);
                size = size - innerElement.AskSize;
                innerElement.AskSize = 0;
                innerElement.LastTrade = data.timestamp;
              }
            }
          }
  
          await this.channel!.broadcast({
            type: topic,
            result: {
              success: true,
              action: "BUY",
              tradePrice: price
            }
          });

          this.subject.next(
            {
              Symbol: data.symbol,
              DataSource: ELEMENT_DATA
            });

          return;
        }

        //It's selling the symbols - probably on the highest seller value (as it's not defined in this poc)
        let symbolElement: SymbolElement | undefined;

        ELEMENT_DATA.forEach((symbol) => {
          if (symbol.Symbol != data.symbol) {
            return;
          }
          if (symbol.Children && symbol.Children.length > 0) {
            symbolElement = symbol.Children.reduce((prev, current) => {
              if (prev.BidPrice && current.BidPrice
                && prev.BidPrice > current.BidPrice) {
                  return prev;
                }

                return current;
            });
          }
        });

        if(symbolElement) {
          symbolElement.BidSize = symbolElement.BidSize + data.quantity;
          symbolElement.LastTrade = data.timestamp;
          await this.channel!.broadcast(
            {
              type: topic,
              result: {
                success: true,
                action: "SELL",
              }
            });
          this.subject.next(
          {
            Symbol: data.symbol,
            DataSource: [...ELEMENT_DATA]
          });
        } else {
          await this.channel!.broadcast(
            {
              type: topic,
              result: {
                success: false,
                action: "SELL",
                error: "Trader is not able to place its symbol for selling."
              }
            });
        }

        return;
      });

      this.listeners.push(listener);

    } catch (err) {
      console.error(err);
    }
  }

  public displayedColumns: string[] = ['Symbol', 'Description', 'AskPrice', 'AskSize', 'BidPrice', 'BidSize', 'LastTrade'];
  public marketData: MatTableDataSource<SymbolElement> = new MatTableDataSource(ELEMENT_DATA);
  public currentRow: SymbolElement | undefined;
  private channel: Channel | undefined;

  public selectSymbol(symbolRow: SymbolElement) {
    this.currentRow === symbolRow ? this.currentRow = undefined : this.currentRow = symbolRow;
  }
}