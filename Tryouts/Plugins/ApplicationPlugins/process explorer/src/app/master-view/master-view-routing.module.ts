/* Morgan Stanley makes this available to you under the Apache License, Version 2.0 (the "License"). You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0. See the NOTICE file distributed with this work for additional information regarding copyright ownership. Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions and limitations under the License. */
import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { MasterViewComponent } from './master-view.component';
import { ConnectionsComponent } from './connections/connections.component';
import { ProcessesComponent } from './processes/processes.component';
import { MemoryChartComponent } from './memory-chart/memory-chart.component';
import { SubsystemComponent } from './subsystems/subsystem.component';

const routes: Routes = [
  { 
    path: '', 
    component: MasterViewComponent, 
    children: [
      { 
        path: '', 
        redirectTo: 'connections', 
        pathMatch: 'full'
      }, { 
        path: 'connections', 
        component: ConnectionsComponent, 
        data: { text: 'Connections'}
      }, { 
        path: 'processes', 
        component: ProcessesComponent, 
        data: { text: 'Processes'}
      }, { 
        path: 'memory-chart', 
        component: MemoryChartComponent, 
        data: { text: 'Memory chart'}
      },
      { 
        path: 'subsystems', 
        component: SubsystemComponent, 
        data: { text: 'Subsystems'}
      }
    ]
  }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class MasterViewRoutingModule {
}
