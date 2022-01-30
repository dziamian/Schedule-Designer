import { FlatTreeControl } from '@angular/cdk/tree';
import { Component, EventEmitter, Input, OnInit, Output, ViewChild } from '@angular/core';
import { MatTreeFlatDataSource } from '@angular/material/tree';
import { Subject, Subscription } from 'rxjs';
import { debounceTime, distinctUntilChanged } from 'rxjs/operators';
import { Filter } from 'src/app/others/Filter';
import { ResourceFlatNode, ResourceItem, ResourceNode } from 'src/app/others/ResourcesTree';
import { ResourceTreeService } from 'src/app/services/ResourceTreeService/resource-tree.service';

@Component({
  selector: 'app-available-resources',
  templateUrl: './available-resources.component.html',
  styleUrls: ['./available-resources.component.css']
})
export class AvailableResourcesComponent implements OnInit {

  @Input() representativeGroups: number[] = [];
  @Input() disabled: boolean;

  @Output() showSchedule: EventEmitter<ResourceItem> = new EventEmitter();
  
  filterValue: string = '';
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
    this.loading = true;

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

  isRepresentative(filter: Filter | null): boolean {
    if (filter == null) {
      return false;
    }
    return this.representativeGroups.some(groupId => filter.GroupsIds.includes(groupId));
  }

  FilterChanged(event: Event): void {
    const value = (<HTMLInputElement>event.target).value;
    this.filterChanged.next(value);
  }

  ClearFilter() {
    this.filterValue = '';
    this.filterChanged.next('');
  }

  Action(node: ResourceNode): void {
    this.showSchedule.emit(node.item);
  }

  ngOnDestroy() {
    this.resourceTreeService.clearFilter();
    this.filterChangedSub.unsubscribe();
  }
}
