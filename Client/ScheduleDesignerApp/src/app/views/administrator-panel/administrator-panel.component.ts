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

/**
 * Komponent zawierający widok panelu administracyjnego.
 */
@Component({
  selector: 'app-administrator-panel',
  templateUrl: './administrator-panel.component.html',
  styleUrls: ['./administrator-panel.component.css']
})
export class AdministratorPanelComponent implements OnInit {

  /** Instancje komponentów wyświetlających drzewka zasobów. */
  @ViewChildren(AdminResourcesComponent) adminTrees!: QueryList<AdminResourcesComponent>

  /** Ustawienia aplikacji. */
  settings: Settings;

  /** 
   * Szczegóły dotyczące wyświetlania komponentu drzewka zasobów 
   * (nagłówek komponentu, typ zasobu do załadowania, czy jest widoczne, 
   * typy zasobów, które należy pominąć, identyfikatory węzłów, które należy pominąć).
  */
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
  
  /** 
   * Dane obszaru roboczego (identyfikator wyświetlanego zasobu, 
   * typ zasobu, rodzaj akcji - "add"/"view"). 
  */
  operatingFieldData:{id: string|undefined, type: string, actionType: string};

  /** Wybrany rezultat z drugiego drzewka zasobów (typ wybranego zasobu, wybrany węzeł drzewka). */
  selectedResult: {type: string, node: ResourceNode} | null;
  
  /** Informuje czy dane zostały załadowane. */
  loading: boolean = true;
  /** Informuje o statusie połączenia z centrum. */
  connectionStatus: boolean = false;
  
  isConnectedSubscription: Subscription;

  constructor(
    private scheduleDesignerApiService: ScheduleDesignerApiService,
    private signalrService: SignalrService,
    private snackBar:MatSnackBar,
  ) { }

  /**
   * Metoda przygotowująca komponent.
   * Pobiera dane niezbędne do wyświetlenia widoku (ustawienia aplikacji).
   */
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
  
  /**
   * Metoda ustawia początkowy widok po wybraniu jednej z dostępnych głównych opcji (Courses, Rooms, Users, itp.).
   * @param type Typ wyświetlanych zasobów w pierwszym drzewku
   * @param header Nagłówek pierwszego drzewka
   * @param visible Określa czy pierwsze drzewko powinno być widoczne
   */
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

  /**
   * Metoda wywoływana w momencie zakończenia wykonywania operacji utworzenia nowego zasobu.
   * Przeładowuje pierwsze drzewko zasobów i resetuje widok.
   * @param id Identyfikator zasobu, który ma zostać wyświetlony w obszarze roboczym
   */
  ForwardAndRefresh(id: string) {
    this.operatingFieldData = {id: id, type: this.operatingFieldData.type, actionType: 'view'};

    this.treeDetails[1].visible = false;

    this.adminTrees.toArray()[0].LoadResources();
  }

  /**
   * Metoda wywoływana w momencie zakończenia wykonywania operacji usuwania zasobu.
   * Przeładowuje pierwsze drzewko zasobów i resetuje widok.
   */
  CloseAndRefresh() {
    this.operatingFieldData = {id: undefined, type: '', actionType: ''};
    
    this.treeDetails[1].visible = false;
    
    this.adminTrees.toArray()[0].LoadResources();
  }

  /**
   * Odebranie rezultatu wyboru z obszaru roboczego i przesłanie go do drugiego drzewka zasobów.
   * Wyłączenie możliwości wybrania po raz drugi tego zasobu.
   * @param event Informacje o identyfikatorach wybranych zasobów oraz ich typ
   */
  OnListAdd(event: {ids: string[], type: string}) {
    if (this.treeDetails[1].type !== event.type) {
      return;
    }

    this.treeDetails[1].excludeIds.push(...event.ids);
  }

  /**
   * Odebranie rezultatu wyboru z obszaru roboczego i przesłanie go do drugiego drzewka zasobów.
   * Włączenie możliwości wybrania po raz drugi tego zasobu.
   * @param event Informacje o identyfikatorach wybranych zasobów oraz ich typ
   * @returns 
   */
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

  /**
   * Metoda wyświetlająca obszar roboczy z podanymi parametrami.
   * @param event Informacje o identyfikatorze zasobu, jego typie i akcji jaka będzie wykonywana ("view"/"add").
   */
  ShowOperatingField(event: {id: string | undefined, type: string, action: string}) {
    this.operatingFieldData = {
      id: event.id, 
      type: event.type, 
      actionType: event.action
    };
    this.treeDetails[1].visible = false;
  }

  /**
   * Odebranie pojedynczego rezultatu wyboru z drugiego drzewka zasobów i przesłanie go do obszaru roboczego.
   * @param event Informacje o typie wybranego zasobu i obiekt wybranego węzła z drzewka
   */
  SendResult(event: {type: string, node: ResourceNode}) {
    this.selectedResult = event;
  }
}
