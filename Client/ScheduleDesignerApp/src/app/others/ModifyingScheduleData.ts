import { CdkDragStart } from "@angular/cdk/drag-drop";
import { MatDialogRef } from "@angular/material/dialog";
import { AddRoomSelectionComponent } from "../components/add-room-selection/add-room-selection.component";
import { RoomSelectionComponent } from "../components/room-selection/room-selection.component";
import { ScheduledChangesViewComponent } from "../components/scheduled-changes-view/scheduled-changes-view.component";
import { CourseEdition } from "./CourseEdition";
import { SelectedCourseEdition } from "./SelectedCourseEdition";

/**
 * Klasa przechowująca informacje o aktualnych stanach w trybie modyfikacji planu zajęć.
 */
export class ModifyingScheduleData {
    /** Zdarzenie obecnie wykonywanego przeciągania zajęć po ekranie. */
    currentDragEvent: CdkDragStart<CourseEdition> | null = null;
    /** Indeksy obecnie rozważanego pola w planie zajęć przez użytkownika. */
    currentDropContainerIndexes: number[];
    /** Określa czy obecnie wykonywane przeciąganie po ekranie się zakończyło. */
    isCurrentDragReleased: boolean = false;
    /** Określa czy przeciąganie po ekranie zostało cofnięte (np. z powodu wystąpienia błędu). */
    isCurrentDragCanceled: boolean = false;
    
    /** Obecnie zaznaczone (wybrane) pozycje w planie przez użytkownika. */
    currentSelectedCourseEdition: SelectedCourseEdition | null = null;

    /** Tablica określająca, które pola w planie powodują konflikty, a które nie (prawda - nie powoduje konfliktów). */
    scheduleSlotsValidity: boolean[][];
    /** Określa czy tablica {@link scheduleSlotsValidity} została ustawiona.*/
    areSlotsValiditySet: boolean = false;
    
    /** Instancja dialogu dodawania nowego pokoju do przedmiotu wyświetlonego na ekranie. */
    currentAddRoomSelectionDialog: MatDialogRef<AddRoomSelectionComponent, any> | null = null;
    /** Instancja dialogu do przeglądania i zarządzania zaplanowanymi zmianami wyświetlonego na ekranie. */
    currentScheduledChangesDialog: MatDialogRef<ScheduledChangesViewComponent, any> | null = null;
    /** Instancja dialogu do wybrania pokoju i wykonania operacji na planie wyświetlonego na ekranie. */
    currentRoomSelectionDialog: MatDialogRef<RoomSelectionComponent, any> | null = null;

    /** Określa czy dany ruch jest możliwy do wykonania, czy tylko do zaplanowania. */
    isCurrentMoveValid: boolean | null = null;
}