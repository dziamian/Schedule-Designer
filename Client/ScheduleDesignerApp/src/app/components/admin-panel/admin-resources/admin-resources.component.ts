import { FlatTreeControl } from '@angular/cdk/tree';
import { Component, EventEmitter, Inject, Input, OnInit, Output } from '@angular/core';
import { MatTreeFlatDataSource } from '@angular/material/tree';
import { Subject, Subscription } from 'rxjs';
import { debounceTime, distinctUntilChanged } from 'rxjs/operators';
import { ResourceFlatNode, ResourceNode } from 'src/app/others/ResourcesTree';
import { ResourceTreeService } from 'src/app/services/ResourceTreeService/resource-tree.service';

@Component({
  selector: 'app-admin-resources',
  templateUrl: './admin-resources.component.html',
  styleUrls: ['./admin-resources.component.css'],
  providers: [{
    provide: 'treeResource', useClass: ResourceTreeService
  }]
})
export class AdminResourcesComponent implements OnInit {

  private _type: string;

  @Input() header: string;
  @Input() actionType: string = 'add';
  @Input() visible: boolean = true;
  @Input() excludeTypes: string[] = [];
  @Input() excludeIds: string[] = [];

  @Input() set type(value: string) {
    this._type = value;
    this.LoadResources();
  } get type(): string {
    return this._type;
  }

  @Output() clicked: EventEmitter<{
    id: string | undefined, type: string, action: string, node: ResourceNode
  }> = new EventEmitter();

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
    @Inject('treeResource') private treeService: ResourceTreeService
  ) { }

  async ngOnInit(): Promise<void> {
    this.loading = true;
    this.filterChangedSub = this.filterChanged.pipe(debounceTime(200), distinctUntilChanged()).subscribe(value => {
      if (value) {
        this.treeService.filterByName(value);
      } else {
        this.treeService.clearFilter();
      }
    });

    this.treeControl = this.treeService.treeControl;
    this.dataSource = this.treeService.dataSource;
    
    this.hasChild = this.treeService.hasChild;
    this.isHidden = this.treeService.isHidden;
    this.isVisible = this.treeService.isVisible;
  }

  public LoadResources() {
    this.loading = true;
    
    this.filterValue = '';
    this.treeService.clearData();
    switch (this._type.toLowerCase()) {
      case 'change-group': {
        this.treeService.setAllGroupsForChange(() => {
          this.loading = false;
        });
      } break;
      case 'coordinators': {
        this.treeService.setAllCoordinators(() => {
          this.loading = false;
        });
      } break;
      case 'courses': {
        this.treeService.setAllCourses(() => {
          this.loading = false;
        });
      } break;
      case 'course-types': {
        this.treeService.setAllCourseTypes(() => {
          this.loading = false;
        });
      } break;
      case 'groups': {
        this.treeService.setAllGroups(() => {
          this.loading = false;
        });
      } break;
      case 'rooms': {
        this.treeService.setAllRooms(() => {
          this.loading = false;
        });
      } break;
      case 'rooms-on-types': {
        this.treeService.setAllRoomsOnTypes(() => {
          this.loading = false;
        });
      } break;
      case 'room-types': {
        this.treeService.setAllRoomTypes(() => {
          this.loading = false;
        });
      } break;
      case 'students': {
        this.treeService.setAllStudents(() => {
          this.loading = false;
        });
      } break;
      case 'users': {
        this.treeService.setAllUsers(() => {
          this.loading = false;
        });
      } break;
      default: {
        this.treeService.setCurrentData();
        this.loading = false;
      } break;
    }
  }

  FilterChanged(event: Event): void {
    const value = (<HTMLInputElement>event.target).value;
    this.filterChanged.next(value);
  }

  ClearFilter() {
    this.filterValue = '';
    this.filterChanged.next('');
  }

  Click(id: string | undefined, type: string, action: string, node: ResourceNode): void {
    this.clicked.emit({id: id, type: type, action: action, node: node});
  }

  ngOnDestroy() {
    this.treeService.clearFilter();
    this.filterChangedSub.unsubscribe();
  }
}
