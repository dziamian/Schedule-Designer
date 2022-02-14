import { FlatTreeControl } from '@angular/cdk/tree';
import { Component, EventEmitter, Input, OnInit, Output, ViewChild } from '@angular/core';
import { MatTreeFlatDataSource } from '@angular/material/tree';
import { Subject, Subscription } from 'rxjs';
import { debounceTime, distinctUntilChanged } from 'rxjs/operators';
import { Filter } from 'src/app/others/Filter';
import { ResourceFlatNode, ResourceItem, ResourceNode } from 'src/app/others/ResourcesTree';
import { ResourceTreeService } from 'src/app/services/ResourceTreeService/resource-tree.service';

/**
 * Komponent odpowiedzialny za wyświetlanie drzewka zasobów 
 * w opcjach związanych z planem zajęć.
 */
@Component({
  selector: 'app-available-resources',
  templateUrl: './available-resources.component.html',
  styleUrls: ['./available-resources.component.css']
})
export class AvailableResourcesComponent implements OnInit {

  /** Identyfikatory grup studenckich, których student jest starostą. */
  @Input() representativeGroups: number[] = [];
  /** Określa czy przyciski wyświetlane razem z węzłami drzewka są wyłączone. */
  @Input() disabled: boolean;

  /** Emiter zdarzenia wyświetlenia planu zajęć dla wybranego zasobu. */
  @Output() showSchedule: EventEmitter<ResourceItem> = new EventEmitter();
  
  /** Wartość pola wyszukiwania. */
  filterValue: string = '';
  /** Strumień, który przechowuje informacje o tym czy 
   * nastąpiła zmiana wartości pola wyszukiwania. 
   */
  filterChanged: Subject<string> = new Subject<string>();
  filterChangedSub: Subscription;

  /** Informuje czy dane zostały załadowane. */
  loading: boolean | null = null;

  treeControl: FlatTreeControl<ResourceFlatNode>;
  dataSource: MatTreeFlatDataSource<ResourceNode, ResourceFlatNode>;
  hasChild: (_: number, _nodeData: ResourceFlatNode) => boolean;
  isHidden: (_: number, _nodeData: ResourceFlatNode) => boolean;
  isVisible: (_: number, _nodeData: ResourceFlatNode) => boolean;

  constructor(
    private resourceTreeService: ResourceTreeService,
  ) { }

  /**
   * Metoda przygotowująca komponent.
   * Rozpoczyna odbieranie bieżących informacji o zmianach w polu wyszukiwania.
   * Inicjalizuje właściwości wyświetlanego drzewka zasobów.
   */
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

  /**
   * Metoda wywoływana w momencie wykrycia zmiany w polu wyszukiwania.
   * @param event Zdarzenie wykrycia zmiany w polu wyszukiwania
   */
  FilterChanged(event: Event): void {
    const value = (<HTMLInputElement>event.target).value;
    this.filterChanged.next(value);
  }

  /**
   * Metoda czyszcząca pole wyszukiwania.
   */
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
