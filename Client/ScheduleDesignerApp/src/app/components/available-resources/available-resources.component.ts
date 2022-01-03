import { FlatTreeControl } from '@angular/cdk/tree';
import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { MatTreeFlatDataSource, MatTreeFlattener } from '@angular/material/tree';
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

  loading: boolean | null = null;

  treeControl: FlatTreeControl<ResourceFlatNode>;
  dataSource: MatTreeFlatDataSource<ResourceNode, ResourceFlatNode>;
  hasChild: (_: number, _nodeData: ResourceFlatNode) => boolean;

  constructor(
    private resourceTreeService: ResourceTreeService,
  ) { }

  ngOnInit(): void {
    this.treeControl = this.resourceTreeService.treeControl;
    this.dataSource = this.resourceTreeService.dataSource;
    this.hasChild = this.resourceTreeService.hasChild;

    this.loading = false;
  }

  Action(node: ResourceNode): void {
    this.showSchedule.emit(node.item);
  }
}
