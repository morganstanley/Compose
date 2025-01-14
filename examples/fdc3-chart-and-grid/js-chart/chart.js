import Chart from 'highcharts/es-modules/Core/Chart/Chart.js';
import ColumnSeries from 'highcharts/es-modules/Series/Column/ColumnSeries.js';
import NoDataToDisplay from 'highcharts/modules/no-data-to-display';
import * as Highcharts from 'highcharts';
import { ContextTypes } from '@finos/fdc3';
import { Intents } from '@finos/fdc3';

let chart;
let intentListener;
let contextListener;
let currentChannel;

NoDataToDisplay(Highcharts);

window.addEventListener('load', async() => {
  chart = Highcharts.chart('container', {

      chart: {
        type: 'column',
      },
      title: {
        text: 'Monthly Sales Data'
      },
      yAxis: {
        minPadding: 0.2,
        maxPadding: 0.2,
        title: {
          text: 'Value',
          margin: 80,
        }
      },
      xAxis: {
        categories: ['Jan','Feb','March','Apr','May','Jun','Jul','Aug','Sep','Oct','Nov','Dec'],
      },
      series: [{
        name: 'Buy',
        data: [] 
      }, {
        name: 'Sell',
        data: []
      }]
    });

    await requestData();
});

window.addEventListener('close', async function(){
  if (this.intentListener) {
    await this.intentListener.unsubscribe();
    await this.contextListener.unsubscribe();
  }
});

async function requestData() {
  (async () => {
    intentListener = await window.fdc3.addIntentListener(Intents.ViewChart, (context, contextMetada) => {
      console.log("Intent was handled by the chart. Received context:", context, ", metadata:", contextMetada);
    });

    currentChannel = await window.fdc3.getCurrentChannel();
  
    if(!currentChannel) {
      await window.fdc3.joinUserChannel("fdc3.channel.1");
    }

    contextListener = await window.fdc3.addContextListener(ContextTypes.Instrument, (context, metadata) => {
      if(context.id) {
        chart.setTitle({ text: "Monthly sales for " + context.id.ticker });
        chart.series[0].setData([]);
        chart.series[1].setData([]);

        const symbolData = symbols.find((element) => element.symbol == context.id.ticker);

        symbolData.buy.forEach(function (p) {
          chart.series[0].addPoint(p, false);
        });
      
        symbolData.sell.forEach(function (p) {
          chart.series[1].addPoint(p, false);
        });

        chart.redraw();
      }
    });

  })();
}

let symbols = [
  {symbol: 'AAPL', sell: [ 62, 93, 114, 140, 150, 161, 191, 206, 224, 255, 286, 295], buy: [18, 49, 62, 110, 134, 162, 166, 210, 215, 277, 290, 297] },
  {symbol: 'IBM', sell: [ 83, 78, 98, 93, 106, 82, 45, 305, 263, 33, 112, 87 ], buy: [49, 71, 106, 129, 144, 63, 89, 15, 203, 58, 115, 32] },
  {symbol: 'TSLA', sell: [ 33, 62, 64, 94, 124, 166, 186, 191, 225, 247, 267, 294], buy: [33, 93, 124, 177, 187, 210, 225, 234, 236, 247, 250, 282] },
  {symbol:'SMSN', sell: [30, 37, 67, 86, 182, 199, 219, 225, 238, 245, 250, 262], buy: [14, 25, 73, 84, 138, 155, 181, 195, 200, 209, 231, 254] },
  {symbol: 'GOOG', sell: [28, 29, 57, 109, 129, 184, 196, 224, 230, 259, 277, 278], buy: [25, 42, 43, 175, 189, 190, 201, 218, 223, 231, 263, 284] }
];