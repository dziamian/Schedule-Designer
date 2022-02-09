import { FlatTreeControl } from '@angular/cdk/tree';
import { Injectable } from '@angular/core';
import { MatTreeFlatDataSource, MatTreeFlattener } from '@angular/material/tree';
import { forkJoin } from 'rxjs';
import { Coordinator, Staff, Student, User, UserBasic } from 'src/app/others/Accounts';
import { CourseEditionInfo, CourseInfo } from 'src/app/others/CourseInfo';
import { Filter } from 'src/app/others/Filter';
import { Group } from 'src/app/others/Group';
import { ResourceFlatNode, ResourceItem, ResourceNode } from 'src/app/others/ResourcesTree';
import { Room } from 'src/app/others/Room';
import { CourseType, RoomType } from 'src/app/others/Types';
import { AdministratorApiService } from '../AdministratorApiService/administrator-api.service';
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

  constructor(
    private scheduleDesignerApiService: ScheduleDesignerApiService,
    private administratorApiService: AdministratorApiService
  ) {
    this.treeFlattener = new MatTreeFlattener(
      this.transformer,
      this.getLevel,
      this.isExpandable,
      this.getChildren
    );
    this.treeControl = new FlatTreeControl<ResourceFlatNode>(this.getLevel, this.isExpandable);
    this.dataSource = new MatTreeFlatDataSource(this.treeControl, this.treeFlattener);
  }

  private setCourseTypes(courseTypes: CourseType[], root: ResourceNode | null = null) {
    const parentNode = new ResourceNode();
    parentNode.item = new ResourceItem();
    parentNode.item.name = 'Course Types';
    parentNode.item.filter = null;
    parentNode.item.type = "course-type";
    parentNode.item.addActionType = "course-type";
    parentNode.children = [];
    if (root == null) {
      this.TREE_DATA.push(parentNode);
    } else {
      root.children.push(parentNode);
    }

    courseTypes.forEach(
      courseType => {
        const resourceNode = new ResourceNode();
        resourceNode.item = new ResourceItem();
        resourceNode.item.id = courseType.CourseTypeId.toString();
        resourceNode.item.name = courseType.Name;
        resourceNode.item.filter = null;
        resourceNode.item.type = "course-type";
        resourceNode.children = [];
        
        parentNode.children.push(resourceNode);
      }
    );
  }

  private setCourses(courseEditions: CourseEditionInfo[], courses: CourseInfo[], courseTypes: Map<number, CourseType>, root: ResourceNode | null = null) {
    const grandParentNode = new ResourceNode();
    grandParentNode.item = new ResourceItem();
    grandParentNode.item.name = 'Courses';
    grandParentNode.item.filter = null;
    grandParentNode.item.type = "course";
    grandParentNode.item.addActionType = "course";
    grandParentNode.children = [];
    if (root == null) {
      this.TREE_DATA.push(grandParentNode);
    } else {
      root.children.push(grandParentNode);
    }

    courses.forEach(
      course => {
        const parentNode = new ResourceNode();
        parentNode.item = new ResourceItem();
        parentNode.item.id = course.CourseId.toString();
        parentNode.item.name = `${course.Name} (${courseTypes.get(course.CourseTypeId)?.Name})`;
        parentNode.item.filter = null;
        parentNode.item.type = "course";
        parentNode.item.addActionType = "course-edition";
        parentNode.children = [];

        grandParentNode.children.push(parentNode);
      }
    );

    courseEditions.forEach(
      courseEdition => {
        const resourceNode = new ResourceNode();
        resourceNode.item = new ResourceItem();
        resourceNode.item.id = `${courseEdition.CourseId.toString()},${courseEdition.CourseEditionId.toString()}`;
        resourceNode.item.name = courseEdition.CourseEditionName;
        resourceNode.item.filter = null;
        resourceNode.item.type = "course-edition";
        resourceNode.children = [];
        
        var index = grandParentNode.children.findIndex(node => node.item.id === courseEdition.CourseId.toString());
        if (index != -1) {
          grandParentNode.children[index].children.push(resourceNode);
        }
      }
    );
  }

  private setCoordinators(coordinators: Coordinator[], idVisible: boolean = false, root: ResourceNode | null = null) {
    const parentNode = new ResourceNode();
    parentNode.item = new ResourceItem();
    parentNode.item.name = 'Coordinators';
    parentNode.item.filter = null;
    parentNode.item.icon = 'school';
    parentNode.children = [];
    if (root == null) {
      this.TREE_DATA.push(parentNode);
    } else {
      root.children.push(parentNode);
    }

    coordinators.forEach(
      coordinator => {
        const resourceNode = new ResourceNode();
        resourceNode.item = new ResourceItem();
        resourceNode.item.id = coordinator.User.UserId.toString();
        resourceNode.item.name = (coordinator.Titles.TitleBefore != null ? `${coordinator.Titles.TitleBefore} ` : '') + 
          `${coordinator.User.LastName.toUpperCase()} ${coordinator.User.FirstName}` + 
          (coordinator.Titles.TitleAfter != null ? ` ${coordinator.Titles.TitleAfter}` : '');
        if (idVisible) {
          resourceNode.item.name += ` (${coordinator.User.UserId})`;
        }
        resourceNode.item.filter = new Filter([coordinator.User.UserId],[],[]);
        resourceNode.item.type = "user";
        resourceNode.children = [];
        
        parentNode.children.push(resourceNode);
      }
    );
  }

  private setAdministrators(staffs: Staff[], idVisible: boolean = false, root: ResourceNode | null = null) {
    const parentNode = new ResourceNode();
    parentNode.item = new ResourceItem();
    parentNode.item.name = 'Administrators';
    parentNode.item.filter = null;
    parentNode.children = [];
    if (root == null) {
      this.TREE_DATA.push(parentNode);
    } else {
      root.children.push(parentNode);
    }

    staffs.forEach(
      staff => {
        if (!staff.IsAdmin) {
          return;
        }
        
        const resourceNode = new ResourceNode();
        resourceNode.item = new ResourceItem();
        resourceNode.item.id = staff.User.UserId.toString();
        resourceNode.item.name = (staff.Titles.TitleBefore != null ? `${staff.Titles.TitleBefore} ` : '') + 
          `${staff.User.FirstName} ${staff.User.LastName}` + 
          (staff.Titles.TitleAfter != null ? ` ${staff.Titles.TitleAfter}` : '');
        if (idVisible) {
          resourceNode.item.name += ` (${staff.User.UserId})`;
        }
        resourceNode.item.filter = null;
        resourceNode.item.type = "user";
        resourceNode.children = [];
        
        parentNode.children.push(resourceNode);
      }
    );
  }

  private setOtherStaffs(staffs: Staff[], excludedUserIds: number[], idVisible: boolean = false, root: ResourceNode | null = null) {
    const parentNode = new ResourceNode();
    parentNode.item = new ResourceItem();
    parentNode.item.name = 'Other Staffs';
    parentNode.item.filter = null;
    parentNode.children = [];
    if (root == null) {
      this.TREE_DATA.push(parentNode);
    } else {
      root.children.push(parentNode);
    }

    staffs.forEach(
      staff => {
        if (staff.IsAdmin || excludedUserIds.includes(staff.User.UserId)) {
          return;
        }
        
        const resourceNode = new ResourceNode();
        resourceNode.item = new ResourceItem();
        resourceNode.item.id = staff.User.UserId.toString();
        resourceNode.item.name = (staff.Titles.TitleBefore != null ? `${staff.Titles.TitleBefore} ` : '') + 
          `${staff.User.FirstName} ${staff.User.LastName}` + 
          (staff.Titles.TitleAfter != null ? ` ${staff.Titles.TitleAfter}` : '');
        if (idVisible) {
          resourceNode.item.name += ` (${staff.User.UserId})`;
        }
        resourceNode.item.filter = null;
        resourceNode.item.type = "user";
        resourceNode.children = [];
        
        parentNode.children.push(resourceNode);
      }
    );
  }

  private setOtherUsers(users: UserBasic[], idVisible: boolean = false, root: ResourceNode | null = null) {
    const parentNode = new ResourceNode();
    parentNode.item = new ResourceItem();
    parentNode.item.name = 'Other Users';
    parentNode.item.filter = null;
    parentNode.children = [];
    if (root == null) {
      this.TREE_DATA.push(parentNode);
    } else {
      root.children.push(parentNode);
    }

    users.forEach(
      user => {
        
        const resourceNode = new ResourceNode();
        resourceNode.item = new ResourceItem();
        resourceNode.item.id = user.UserId.toString();
        resourceNode.item.name = user.FullName;
        if (idVisible) {
          resourceNode.item.name += ` (${user.UserId})`;
        }
        resourceNode.item.filter = null;
        resourceNode.item.type = "user";
        resourceNode.children = [];
        
        parentNode.children.push(resourceNode);
      }
    );
  }

  private setStudents(students: Student[], idVisible: boolean = false, root: ResourceNode | null = null) {
    const parentNode = new ResourceNode();
    parentNode.item = new ResourceItem();
    parentNode.item.name = 'Students';
    parentNode.item.filter = null;
    parentNode.children = [];
    if (root == null) {
      this.TREE_DATA.push(parentNode);
    } else {
      root.children.push(parentNode);
    }

    students.forEach(
      student => {
        const resourceNode = new ResourceNode();
        resourceNode.item = new ResourceItem();
        resourceNode.item.id = student.User.UserId.toString();
        resourceNode.item.name = (student.Titles.TitleBefore != null ? `${student.Titles.TitleBefore} ` : '') + 
          `${student.User.FirstName} ${student.User.LastName}` + 
          (student.Titles.TitleAfter != null ? ` ${student.Titles.TitleAfter}` : '');
        if (idVisible) {
          resourceNode.item.name += ` (${student.User.UserId})`;
        }
        resourceNode.item.filter = null;
        resourceNode.item.type = "user";
        resourceNode.children = [];
        
        parentNode.children.push(resourceNode);
      }
    );
  }

  private groupNodeExist(node: ResourceNode, groupId: string): boolean {
    for (var child in node.children) {
      if (node.children[child].item.id === groupId) {
        return true;
      }
    }
    return false;
  }

  private createGroupNode(group: Group, parentNode: ResourceNode, groupsMap: Map<number, Group>) {
    const resourceNode = new ResourceNode();
    resourceNode.item = new ResourceItem();
    resourceNode.item.id = group.GroupId.toString();
    resourceNode.item.name = group.FullName;
    resourceNode.item.filter = new Filter([],[group.GroupId],[]);
    resourceNode.item.type = "group";
    resourceNode.item.addActionType = "group";
    resourceNode.children = [];
    
    if (group.ParentGroupId == null) {
      if (!this.groupNodeExist(parentNode, resourceNode.item.id)) {
        parentNode.children.push(resourceNode);
      }
    } else {
      const foundResourceNode = this.searchNodeForGroup(parentNode, group);
      if (foundResourceNode == null) {
        const notFoundGroup = groupsMap.get(group.ParentGroupId);
        if (!notFoundGroup) {
          return;
        }
        
        this.createGroupNode(notFoundGroup, parentNode, groupsMap);
        const createdResourceNode = this.searchNodeForGroup(parentNode, group);
        if (createdResourceNode != null) {
          if (!this.groupNodeExist(createdResourceNode, resourceNode.item.id)) {
            resourceNode.item.name = createdResourceNode.item.name + resourceNode.item.name;
            resourceNode.item.filter.GroupsIds.push(...createdResourceNode.item.filter?.GroupsIds!);
            createdResourceNode.children.push(resourceNode);
          }
        } 
      } else {
        if (!this.groupNodeExist(foundResourceNode, resourceNode.item.id)) {
          resourceNode.item.name = foundResourceNode.item.name + resourceNode.item.name;
          resourceNode.item.filter.GroupsIds.push(...foundResourceNode.item.filter?.GroupsIds!);
          foundResourceNode.children.push(resourceNode);
        }
      }
    }
  }

  private setGroups(groups: Group[], parentMark: string | null = null,  root: ResourceNode | null = null) {
    const parentNode = new ResourceNode();
    parentNode.item = new ResourceItem();
    if (parentMark) {
      parentNode.item.id = parentMark;
      parentNode.item.type = "group";
    }
    parentNode.item.name = 'Groups';
    parentNode.item.filter = null;
    parentNode.item.icon = 'group';
    parentNode.item.addActionType = "group";
    parentNode.children = [];
    if (root == null) {
      this.TREE_DATA.push(parentNode);
    } else {
      root.children.push(parentNode);
    }

    const groupsMap = new Map<number, Group>();

    groups.forEach(group => groupsMap.set(group.GroupId, group));

    for (var i = 0; i < groups.length; ++i) {
      const group = groups[i];
      
      this.createGroupNode(group, parentNode, groupsMap);
    }
  }

  private setRoomTypes(roomTypes: RoomType[], label: string = 'Room Types', root: ResourceNode | null = null) {
    const parentNode = new ResourceNode();
    parentNode.item = new ResourceItem();
    parentNode.item.name = label;
    parentNode.item.filter = null;
    parentNode.item.addActionType = "room-type";
    parentNode.children = [];
    if (root == null) {
      this.TREE_DATA.push(parentNode);
    } else {
      root.children.push(parentNode);
    }

    roomTypes.forEach(
      roomType => {
        const resourceNode = new ResourceNode();
        resourceNode.item = new ResourceItem();
        resourceNode.item.id = roomType.RoomTypeId.toString();
        resourceNode.item.name = roomType.Name;
        resourceNode.item.filter = null;
        resourceNode.item.type = "room-type";
        resourceNode.children = [];
        
        parentNode.children.push(resourceNode);
      }
    );
  }

  private setRooms(rooms: Room[], root: ResourceNode | null = null) {
    const parentNode = new ResourceNode();
    parentNode.item = new ResourceItem();
    parentNode.item.name = 'Rooms';
    parentNode.item.filter = null;
    parentNode.item.icon = 'meeting_room';
    parentNode.item.addActionType = "room";
    parentNode.children = [];
    if (root == null) {
      this.TREE_DATA.push(parentNode);
    } else {
      root.children.push(parentNode);
    }

    rooms.forEach(
      room => {
        const resourceNode = new ResourceNode();
        resourceNode.item = new ResourceItem();
        resourceNode.item.id = room.RoomId.toString();
        resourceNode.item.name = room.Name;
        resourceNode.item.filter = new Filter([],[],[room.RoomId]);
        resourceNode.item.type = "room";
        resourceNode.children = [];
        
        parentNode.children.push(resourceNode);
      }
    );
  }

  private setRoomsOnTypes(rooms: Room[], root: ResourceNode) {
    rooms.forEach(
      room => {
        const resourceNode = new ResourceNode();
        resourceNode.item = new ResourceItem();
        resourceNode.item.id = room.RoomId.toString();
        resourceNode.item.name = room.Name;
        resourceNode.item.filter = new Filter([],[],[room.RoomId]);
        resourceNode.item.type = "room";
        resourceNode.children = [];
        
        var index = root.children.findIndex(node => node.item.id === room.RoomType.RoomTypeId.toString());
        if (index != -1) {
          root.children[index].children.push(resourceNode);
        }
      }
    );
  }

  private searchNodeForGroup(node: ResourceNode, group: Group): ResourceNode | null {
    const nodeChildren = node.children;
    const nodeChildrenLength = nodeChildren.length;
    if (nodeChildrenLength == 0) {
      return null;
    }

    for (var child in nodeChildren) {
      if (nodeChildren[child].item.filter?.GroupsIds.includes(group.ParentGroupId!)) {
        return nodeChildren[child];
      } else {
        const result = this.searchNodeForGroup(nodeChildren[child], group);
        if (result != null) {
          return result;
        }
      }
    }
    return null;
  }

  public setSchedule(callback: () => void) {
    forkJoin([
      this.scheduleDesignerApiService.GetCoordinators(),
      this.scheduleDesignerApiService.GetGroups(),
      this.scheduleDesignerApiService.GetRooms()
    ]).subscribe(([coordinators, groups, rooms]) => {
      this.setCoordinators(coordinators);
      this.setGroups(groups);
      this.setRooms(rooms);

      this.setCurrentData();
      callback();
    });
  }

  public setMyGroups(callback: () => void) {
    forkJoin([
      this.scheduleDesignerApiService.GetMyGroups()
    ]).subscribe(([groups]) => {
      this.setGroups(groups);

      this.setCurrentData();
      callback();
    });
  }

  public setAllCourseTypes(callback: () => void) {
    forkJoin([
      this.scheduleDesignerApiService.GetCourseTypes()
    ]).subscribe(([courseTypes]) => {
      this.setCourseTypes(Array.from(courseTypes, ([key, value]) => (value)));
      
      this.setCurrentData();
      callback();
    });
  }

  public setAllCourses(callback: () => void) {
    this.scheduleDesignerApiService.GetSettings().subscribe(settings => {
      forkJoin([
        this.scheduleDesignerApiService.GetCourseTypes(),
        this.scheduleDesignerApiService.GetCoursesInfo(),
        this.scheduleDesignerApiService.GetCourseEditionsInfo(settings)
      ]).subscribe(([courseTypes, coursesInfo, courseEditionsInfo]) => {
        this.setCourseTypes(Array.from(courseTypes, ([key, value]) => (value)));
        this.setCourses(courseEditionsInfo, coursesInfo, courseTypes);
        
        this.setCurrentData();
        callback();
      });
    });
  }

  public setAllGroups(callback: () => void) {
    forkJoin([
      this.scheduleDesignerApiService.GetGroups()
    ]).subscribe(([groups]) => {
      this.setGroups(groups);

      this.setCurrentData();
      callback();
    });
  }

  public setAllGroupsForChange(callback: () => void) {
    forkJoin([
      this.scheduleDesignerApiService.GetGroups()
    ]).subscribe(([groups]) => {
      this.setGroups(groups, 'root');

      this.setCurrentData();
      callback();
    });
  }

  public setAllRooms(callback: () => void) {
    forkJoin([
      this.scheduleDesignerApiService.GetRoomTypes(),
      this.scheduleDesignerApiService.GetRooms()
    ]).subscribe(([roomTypes, rooms]) => {
      this.setRoomTypes(Array.from(roomTypes, ([key, value]) => (value)));
      this.setRooms(rooms);

      this.setCurrentData();
      callback();
    });
  }

  public setAllRoomsOnTypes(callback: () => void) {
    forkJoin([
      this.scheduleDesignerApiService.GetRoomTypes(),
      this.scheduleDesignerApiService.GetRooms()
    ]).subscribe(([roomTypes, rooms]) => {
      this.setRoomTypes(Array.from(roomTypes, ([key, value]) => (value)), 'Rooms');
      this.setRoomsOnTypes(rooms, this.TREE_DATA[0]);

      this.setCurrentData();
      callback();
    });
  }

  public setAllRoomTypes(callback: () => void) {
    forkJoin([
      this.scheduleDesignerApiService.GetRoomTypes(),
    ]).subscribe(([roomTypes]) => {
      this.setRoomTypes(Array.from(roomTypes, ([key, value]) => (value)));

      this.setCurrentData();
      callback();
    });
  }

  public setAllCoordinators(callback: () => void) {
    this.scheduleDesignerApiService.GetCoordinators().subscribe(coordinators => {
      this.setCoordinators(coordinators, true);

      this.setCurrentData();
      callback();
    });
  }

  public setAllStudents(callback: () => void) {
    this.administratorApiService.GetStudents().subscribe(students => {
      this.setStudents(students, true);

      this.setCurrentData();
      callback();
    });
  }

  public setAllUsers(callback: () => void) {
    forkJoin([
      this.scheduleDesignerApiService.GetCoordinators(),
      this.administratorApiService.GetStaffs(),
      this.administratorApiService.GetStudents(),
      this.administratorApiService.GetOtherUsers()
    ]).subscribe(([coordinators, staffs, students, otherUsers]) => {
      const usersNode = new ResourceNode();
      usersNode.item = new ResourceItem();
      usersNode.item.name = "Users";
      usersNode.item.addActionType = "user";
      usersNode.children = [];

      this.TREE_DATA.push(usersNode);
      this.setCoordinators(coordinators, true, usersNode);
      this.setAdministrators(staffs, true, usersNode);
      this.setOtherStaffs(staffs, coordinators.map(c => c.User.UserId), true, usersNode);
      this.setOtherUsers(otherUsers, true, usersNode);

      this.setStudents(students, true, usersNode);

      this.setCurrentData();
      callback();
    });
  }

  public setCurrentData() {
    this.dataSource.data = this.TREE_DATA;
  }

  public clearData() {
    this.TREE_DATA = [];
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

  private markChilds(flatNode: ResourceFlatNode) {
    var i = this.treeControl.dataNodes.findIndex(x => x == flatNode);
    var currentLevel = this.treeControl.dataNodes[i].level;

    while (true) {
      var nextNode = this.treeControl.dataNodes[i + 1];
      if (nextNode == undefined || nextNode.level <= currentLevel) {
        return;
      }
      nextNode.visible = true;
      ++i;
    }
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
      this.markChilds(x);
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
