import { Component, OnInit, QueryList, ViewChildren } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';
import { HubConnectionState } from '@microsoft/signalr';
import { Subscription } from 'rxjs';
import { skip } from 'rxjs/operators';
import { AdminResourcesComponent } from 'src/app/components/admin-panel/admin-resources/admin-resources.component';
import { ResourceNode } from 'src/app/others/ResourcesTree';
import { Settings } from 'src/app/others/Settings';
import { ScheduleDesignerApiService } from 'src/app/services/ScheduleDesignerApiService/schedule-designer-api.service';
import { SignalrService } from 'src/app/services/SignalrService/signalr.service';

@Component({
  selector: 'app-administrator-panel',
  templateUrl: './administrator-panel.component.html',
  styleUrls: ['./administrator-panel.component.css']
})
export class AdministratorPanelComponent implements OnInit {

  @ViewChildren(AdminResourcesComponent) adminTrees!: QueryList<AdminResourcesComponent>

  settings: Settings;

  treeDetails: Array<{
    header: string,
    type: string,
    visible: boolean,
    excludeTypes: string[], 
    excludeIds: string[]
  }> = [
    {header: 'All Courses', type: 'courses', visible: true, excludeTypes: [], excludeIds: []},
    {header: '', type: '', visible: false, excludeTypes: [], excludeIds: []}
  ];
  
  operatingFieldData:{id: string|undefined, type: string, actionType: string};

  selectedResult: {type: string, node: ResourceNode} | null;

  selectedFile:File;
  
  loading: boolean = true;
  connectionStatus: boolean = false;
  
  isConnectedSubscription: Subscription;

  constructor(
    private scheduleDesignerApiService: ScheduleDesignerApiService,
    private signalrService: SignalrService,
    private snackBar:MatSnackBar,
  ) { }

  ngOnInit(): void {
    this.isConnectedSubscription = this.signalrService.isConnected.pipe(skip(1)).subscribe((status) => {
      this.connectionStatus = status;
      if (!status && !this.signalrService.connectionIntentionallyStopped) {
        this.snackBar.open("Connection with server has been lost. Please refresh the page to possibly reconnect.", "OK");
      }
    });

    this.connectionStatus = this.signalrService.connection?.state == HubConnectionState.Connected;

    this.scheduleDesignerApiService.GetSettings().subscribe(settings => {
      this.settings = settings;

      this.loading = false;
    });
  }
  
  SetInitialContent(type: string, header: string, visible: boolean) {
    this.selectedResult = null;
    this.treeDetails[0].type = type;
    this.treeDetails[0].header = header;
    this.operatingFieldData = {id: undefined, type: '', actionType: ''};
    if (type === 'settings' || type === 'import' || type === 'export' || type === 'clear') {
      this.operatingFieldData = {id: undefined, type: type, actionType: 'view'};
    }
    this.treeDetails[0].visible = visible;
    this.treeDetails[1].visible = false;
  }

  ForwardAndRefresh(id: string) {
    this.operatingFieldData = {id: id, type: this.operatingFieldData.type, actionType: 'view'};

    this.treeDetails[1].visible = false;

    this.adminTrees.toArray()[0].LoadResources();
  }

  CloseAndRefresh() {
    this.operatingFieldData = {id: undefined, type: '', actionType: ''};
    
    this.treeDetails[1].visible = false;
    
    this.adminTrees.toArray()[0].LoadResources();
  }

  OnListAdd(event: {ids: string[], type: string}) {
    if (this.treeDetails[1].type !== event.type) {
      return;
    }

    this.treeDetails[1].excludeIds.push(...event.ids);
  }

  OnListRemove(event: {ids: string[], type: string}) {
    if (this.treeDetails[1].type !== event.type) {
      return;
    }
    
    for (var id in event.ids) {
      const index = this.treeDetails[1].excludeIds.findIndex(e => e == event.ids[id]);
      if (index != -1) {
        this.treeDetails[1].excludeIds.splice(index, 1);
      }
    }
  }

  SetAdditionalContent(event: {type: string, header: string, visible: boolean, excludeTypes: string[], excludeIds: string[]}) {
    this.treeDetails[1].type = event.type;
    this.treeDetails[1].header = event.header;
    this.treeDetails[1].visible = event.visible;
    this.treeDetails[1].excludeTypes = event.excludeTypes;
    this.treeDetails[1].excludeIds = event.excludeIds;
  }

  ForceRefreshTree(index: number) {
    this.adminTrees.toArray()[index].LoadResources();
  }

  ForceRefreshSettings() {
    this.scheduleDesignerApiService.GetSettings().subscribe(settings => {
      this.settings = settings;
    });
  }

  ShowOperatingField(event: {id: string | undefined, type: string, action: string}) {
    this.operatingFieldData = {
      id: event.id, 
      type: event.type, 
      actionType: event.action
    };
    this.treeDetails[1].visible = false;
  }

  SendResult(event: {type: string, node: ResourceNode}) {
    this.selectedResult = event;
  }
}
