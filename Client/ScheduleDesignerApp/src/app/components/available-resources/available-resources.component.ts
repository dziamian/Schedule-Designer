import { FlatTreeControl } from '@angular/cdk/tree';
import { Component, OnInit } from '@angular/core';
import { MatTreeFlatDataSource, MatTreeFlattener } from '@angular/material/tree';
import { Coordinator } from 'src/app/others/Accounts';
import { Filter } from 'src/app/others/Filter';
import { Group } from 'src/app/others/Group';
import { ResourceFlatNode, ResourceNode } from 'src/app/others/ResourcesTree';
import { Room } from 'src/app/others/Room';
import { ScheduleDesignerApiService } from 'src/app/services/ScheduleDesignerApiService/schedule-designer-api.service';

@Component({
  selector: 'app-available-resources',
  templateUrl: './available-resources.component.html',
  styleUrls: ['./available-resources.component.css']
})
export class AvailableResourcesComponent implements OnInit {

  loading: boolean | null = null;

  flatNodeMap: Map<ResourceFlatNode, ResourceNode> = new Map<ResourceFlatNode, ResourceNode>();
  nestedNodeMap: Map<ResourceNode, ResourceFlatNode> = new Map<ResourceNode, ResourceFlatNode>();

  treeControl: FlatTreeControl<ResourceFlatNode>;
  treeFlattener: MatTreeFlattener<ResourceNode, ResourceFlatNode>;
  dataSource: MatTreeFlatDataSource<ResourceNode, ResourceFlatNode>;

  coordinators:Coordinator[];
  groups:Group[];
  rooms:Room[];

  TREE_DATA = [
    {name: 'Coordinators', filter: null, icon: 'school', children: [
      {name: 'RYBACZEWSKA-BŁAŻEJOWSKA M. dr hab. inż. Prof. PŚK', filter: new Filter([],[],[]), children: null},
      {name: 'CHOMICZ-KOWALSKA Anna dr hab. inż. prof. PŚk', filter: new Filter([],[],[]), children: null}
    ]},
    {name: 'Groups', filter: null, icon: 'group', children: [
      {name: 'Testowa', filter: new Filter([],[],[]), children: null},
      {name: '3ID', filter: new Filter([],[],[]), children: [
        {name: '3ID12', filter: new Filter([],[],[]), children: [
          {name: '3ID12A', filter: new Filter([],[],[]), children: null},
          {name: '3ID12B', filter: new Filter([],[],[]), children: null}
        ]}
      ]}
    ]},
    {name: 'Rooms', filter: null, icon: 'meeting_room', children: [
      {name: '1.01D', filter: new Filter([],[],[]), children: null},
      {name: '1.02D', filter: new Filter([],[],[]), children: null}
    ]}
  ]

  private buildTree(obj: {[key: string]: any}, level: number): ResourceNode[] {
    return Object.keys(obj).reduce<ResourceNode[]>((accumulator, key) => {
      const value = obj[key];
      const node = new ResourceNode();
      node.item = {name: value.name, filter: value.filter, icon: value.icon ?? ''};

      if (value.children != null && typeof value.children === 'object') {
        node.children = this.buildTree(value.children, level + 1);
      }

      return accumulator.concat(node);
    }, []);
  }

  constructor(
    private scheduleDesignerApiService: ScheduleDesignerApiService,
  ) {
    this.treeFlattener = new MatTreeFlattener(
      this.transformer,
      this.getLevel,
      this.isExpandable,
      this.getChildren
    );
    this.treeControl = new FlatTreeControl<ResourceFlatNode>(this.getLevel, this.isExpandable);
    this.dataSource = new MatTreeFlatDataSource(this.treeControl, this.treeFlattener);
    this.dataSource.data = this.buildTree(this.TREE_DATA, 0);
    this.loading = false;
  }

  getLevel = (node: ResourceFlatNode) => node.level;

  isExpandable = (node: ResourceFlatNode) => node.expandable;

  getChildren = (node: ResourceNode): ResourceNode[] => node.children;

  hasChild = (_: number, _nodeData: ResourceFlatNode) => _nodeData.expandable;

  hasNoContent = (_: number, _nodeData: ResourceFlatNode) => _nodeData.item.name === '';

  transformer = (node: ResourceNode, level: number) => {
    const existingNode = this.nestedNodeMap.get(node);
    const flatNode = (existingNode && existingNode.item === node.item) ? existingNode : new ResourceFlatNode();
    flatNode.item = node.item;
    flatNode.level = level;
    flatNode.expandable = !!node.children?.length;
    this.flatNodeMap.set(flatNode, node);
    this.nestedNodeMap.set(node, flatNode);
    return flatNode;
  };

  ngOnInit(): void {
    
  }

}
