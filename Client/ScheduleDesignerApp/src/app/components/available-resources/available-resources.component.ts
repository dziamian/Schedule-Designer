import { FlatTreeControl } from '@angular/cdk/tree';
import { Component, EventEmitter, OnInit, Output, ViewChild } from '@angular/core';
import { MatTree, MatTreeFlatDataSource } from '@angular/material/tree';
import { Subject, Subscription } from 'rxjs';
import { debounceTime, distinctUntilChanged } from 'rxjs/operators';
import { Coordinator } from 'src/app/others/Accounts';
import { Filter } from 'src/app/others/Filter';
import { Group } from 'src/app/others/Group';
import { ResourceFlatNode, ResourceItem, ResourceNode } from 'src/app/others/ResourcesTree';
import { Room } from 'src/app/others/Room';
import { ResourceTreeService } from 'src/app/services/ResourceTreeService/resource-tree.service';

@Component({
  selector: 'app-available-resources',
  templateUrl: './available-resources.component.html',
  styleUrls: ['./available-resources.component.css']
})
export class AvailableResourcesComponent implements OnInit {

  @Output() showSchedule: EventEmitter<ResourceItem> = new EventEmitter();
  
  filterChanged: Subject<string> = new Subject<string>();
  filterChangedSub: Subscription;

  loading: boolean | null = null;

  treeControl: FlatTreeControl<ResourceFlatNode>;
  dataSource: MatTreeFlatDataSource<ResourceNode, ResourceFlatNode>;
  hasChild: (_: number, _nodeData: ResourceFlatNode) => boolean;
  isHidden: (_: number, _nodeData: ResourceFlatNode) => boolean;
  isVisible: (_: number, _nodeData: ResourceFlatNode) => boolean;

  constructor(
    private resourceTreeService: ResourceTreeService,
  ) { }

  ngOnInit(): void {
    this.filterChangedSub = this.filterChanged.pipe(debounceTime(200), distinctUntilChanged()).subscribe(value => {
      if (value) {
        this.resourceTreeService.filterByName(value);
      } else {
        this.resourceTreeService.clearFilter();
      }
    });

    this.treeControl = this.resourceTreeService.treeControl;
    this.dataSource = this.resourceTreeService.dataSource;
    
    this.hasChild = this.resourceTreeService.hasChild;
    this.isHidden = this.resourceTreeService.isHidden;
    this.isVisible = this.resourceTreeService.isVisible;

    this.loading = false;
  }

  FilterChanged(event: Event): void {
    const value = (<HTMLInputElement>event.target).value;
    this.filterChanged.next(value);
  }

  Action(node: ResourceNode): void {
    this.showSchedule.emit(node.item);
  }

  ngOnDestroy() {
    this.resourceTreeService.clearFilter();
    this.filterChangedSub.unsubscribe();
  }
}
