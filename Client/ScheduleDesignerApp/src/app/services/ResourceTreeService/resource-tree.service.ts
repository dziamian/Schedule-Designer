import { FlatTreeControl } from '@angular/cdk/tree';
import { Injectable } from '@angular/core';
import { MatTreeFlatDataSource, MatTreeFlattener } from '@angular/material/tree';
import { forkJoin } from 'rxjs';
import { Coordinator } from 'src/app/others/Accounts';
import { Filter } from 'src/app/others/Filter';
import { Group } from 'src/app/others/Group';
import { ResourceFlatNode, ResourceItem, ResourceNode } from 'src/app/others/ResourcesTree';
import { Room } from 'src/app/others/Room';
import { ScheduleDesignerApiService } from '../ScheduleDesignerApiService/schedule-designer-api.service';

@Injectable({
  providedIn: 'root'
})
export class ResourceTreeService {

  flatNodeMap: Map<ResourceFlatNode, ResourceNode> = new Map<ResourceFlatNode, ResourceNode>();
  nestedNodeMap: Map<ResourceNode, ResourceFlatNode> = new Map<ResourceNode, ResourceFlatNode>();

  treeControl: FlatTreeControl<ResourceFlatNode>;
  treeFlattener: MatTreeFlattener<ResourceNode, ResourceFlatNode>;
  dataSource: MatTreeFlatDataSource<ResourceNode, ResourceFlatNode>;

  TREE_DATA: ResourceNode[] = [];

  private buildTree(obj: {[key: string]: any}, level: number): ResourceNode[] {
    return Object.keys(obj).reduce<ResourceNode[]>((accumulator, key) => {
      const value = obj[key];
      const node = new ResourceNode();
      node.item = {name: value.item.name, filter: value.item.filter, icon: value.item.icon ?? ''};

      if (value.children != null && typeof value.children === 'object' && value.children.length > 0) {
        node.children = this.buildTree(value.children, level + 1);
      }

      return accumulator.concat(node);
    }, []);
  }

  private setCoordinators(coordinators: Coordinator[]) {
    const parentNode = new ResourceNode();
    parentNode.item = new ResourceItem();
    parentNode.item.name = 'Coordinators';
    parentNode.item.filter = null;
    parentNode.item.icon = 'school';
    parentNode.children = [];
    this.TREE_DATA.push(parentNode);

    coordinators.forEach(
      coordinator => {
        const resourceNode = new ResourceNode();
        resourceNode.item = new ResourceItem();
        resourceNode.item.name 
          = `${coordinator.Titles.TitleBefore ?? ''} ${coordinator.LastName.toUpperCase()} ${coordinator.FirstName} ${coordinator.Titles.TitleAfter ?? ''}`;
        resourceNode.item.filter = new Filter([coordinator.UserId],[],[]);
        resourceNode.children = [];
        
        parentNode.children.push(resourceNode);
      }
    );
  }

  private setGroups(groups: Group[]) {
    const parentNode = new ResourceNode();
    parentNode.item = new ResourceItem();
    parentNode.item.name = 'Groups';
    parentNode.item.filter = null;
    parentNode.item.icon = 'group';
    parentNode.children = [];
    this.TREE_DATA.push(parentNode);

    for (var i = 0; i < groups.length; ++i) {
      const group = groups[i];
      
      const resourceNode = new ResourceNode();
      resourceNode.item = new ResourceItem();
      resourceNode.item.name = group.FullName;
      resourceNode.item.filter = new Filter([],[group.GroupId],[]);
      resourceNode.children = [];
      
      if (group.ParentGroupId == null) {
        parentNode.children.push(resourceNode);
      } else {
        const foundResourceNode = this.searchNodeForGroup(this.TREE_DATA[1], group);
        if (foundResourceNode == null) {
          parentNode.children.push(resourceNode);
        } else {
          resourceNode.item.name = foundResourceNode.item.name + resourceNode.item.name;
          resourceNode.item.filter.GroupsIds.push(...foundResourceNode.item.filter?.GroupsIds!);
          foundResourceNode.children.push(resourceNode);
        }
      }
    }
  }

  private setRooms(rooms: Room[]) {
    const parentNode = new ResourceNode();
    parentNode.item = new ResourceItem();
    parentNode.item.name = 'Rooms';
    parentNode.item.filter = null;
    parentNode.item.icon = 'meeting_room';
    parentNode.children = [];
    this.TREE_DATA.push(parentNode);

    rooms.forEach(
      room => {
        const resourceNode = new ResourceNode();
        resourceNode.item = new ResourceItem();
        resourceNode.item.name = room.Name;
        resourceNode.item.filter = new Filter([],[],[room.RoomId]);
        resourceNode.children = [];
        
        parentNode.children.push(resourceNode);
      }
    );
  }

  private searchNodeForGroup(node: ResourceNode, group: Group): ResourceNode | null {
    const nodeChildren = node.children;
    const nodeChildrenLength = nodeChildren.length;
    if (nodeChildrenLength == 0) {
      return null;
    }

    for (var i = 0; i < nodeChildren.length; ++i) {
      if (nodeChildren[i].item.filter?.GroupsIds.includes(group.ParentGroupId!)) {
        return nodeChildren[i];
      } else {
        const result = this.searchNodeForGroup(nodeChildren[i], group);
        if (result != null) {
          return result;
        }
      }
    }
    return null;
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

    forkJoin([
      this.scheduleDesignerApiService.GetCoordinators(),
      this.scheduleDesignerApiService.GetGroups(),
      this.scheduleDesignerApiService.GetRooms()
    ]).subscribe(([coordinators, groups, rooms]) => {
      this.setCoordinators(coordinators);
      this.setGroups(groups);
      this.setRooms(rooms);

      this.dataSource.data = this.buildTree(this.TREE_DATA, 0);
    });
  }

  private markParents(flatNode: ResourceFlatNode) {
    var i = this.treeControl.dataNodes.findIndex(x => x == flatNode);
    var previousLevel = this.treeControl.dataNodes[i].level - 1;
    if (i == 0 || previousLevel < 0) {
      return;
    }

    do {
      var previousNode = this.treeControl.dataNodes[i - 1];
      if (previousLevel == previousNode.level) {
        previousNode.visible = true;
        --previousLevel;
      }
      --i;
    } while (previousNode.level != 0);
  }

  public filterByName(term: string) {
    const filteredItems = this.treeControl.dataNodes.filter(
      x => x.item.name.toLowerCase().indexOf(term.toLowerCase()) === -1
    );
    filteredItems.map(x => {
      x.visible = false;
    });

    const visibleItems = this.treeControl.dataNodes.filter(
      x => x.item.name.toLowerCase().indexOf(term.toLowerCase()) > -1
    );
    visibleItems.map(x => {
      x.visible = true;
      this.markParents(x);
    });

    const levelZeroItems = this.treeControl.dataNodes.filter(
      x => x.level == 0
    );
    levelZeroItems.forEach(
      item => {
        if (this.treeControl.isExpanded(item)) {
          this.treeControl.collapse(item);
          this.treeControl.expand(item);
        }
      }
    );
  }

  public clearFilter() {
    this.treeControl.dataNodes.forEach(x => x.visible = true);

    const levelZeroItems = this.treeControl.dataNodes.filter(
      x => x.level == 0
    );
    levelZeroItems.forEach(
      item => {
        if (this.treeControl.isExpanded(item)) {
          this.treeControl.collapse(item);
          this.treeControl.expand(item);
        }
      }
    );
  }

  getLevel = (node: ResourceFlatNode) => node.level;

  isExpandable = (node: ResourceFlatNode) => node.expandable;

  getChildren = (node: ResourceNode): ResourceNode[] => node.children;

  hasChild = (_: number, _nodeData: ResourceFlatNode) => _nodeData.expandable;

  isHidden = (_: number, _nodeData: ResourceFlatNode) => !_nodeData.visible;

  isVisible = (_: number, _nodeData: ResourceFlatNode) => _nodeData.visible;

  hasNoContent = (_: number, _nodeData: ResourceFlatNode) => _nodeData.item.name === '';

  transformer = (node: ResourceNode, level: number) => {
    const existingNode = this.nestedNodeMap.get(node);
    const flatNode = (existingNode && existingNode.item === node.item) ? existingNode : new ResourceFlatNode();
    flatNode.item = node.item;
    flatNode.level = level;
    flatNode.expandable = !!node.children?.length;
    flatNode.visible = true;
    
    this.flatNodeMap.set(flatNode, node);
    this.nestedNodeMap.set(node, flatNode);
    return flatNode;
  };
}
