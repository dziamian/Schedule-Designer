import { CdkDragStart } from "@angular/cdk/drag-drop";
import { MatDialogRef } from "@angular/material/dialog";
import { AddRoomSelectionComponent } from "../components/add-room-selection/add-room-selection.component";
import { RoomSelectionComponent } from "../components/room-selection/room-selection.component";
import { ScheduledChangesViewComponent } from "../components/scheduled-changes-view/scheduled-changes-view.component";
import { CourseEdition } from "./CourseEdition";
import { SelectedCourseEdition } from "./SelectedCourseEdition";

export class ModifyingScheduleData {
    currentDragEvent: CdkDragStart<CourseEdition> | null = null;
    currentDropContainerIndexes: number[];
    isCurrentDragReleased: boolean = false;
    isCurrentDragCanceled: boolean = false;
    
    currentSelectedCourseEdition: SelectedCourseEdition | null = null;
    currentSelectedDropContainerId: string = "";

    scheduleSlotsValidity: boolean[][];
    areSlotsValiditySet: boolean = false;
    
    currentAddRoomSelectionDialog: MatDialogRef<AddRoomSelectionComponent, any> | null = null;
    currentScheduledChangesDialog: MatDialogRef<ScheduledChangesViewComponent, any> | null = null;
    currentRoomSelectionDialog: MatDialogRef<RoomSelectionComponent, any> | null = null;

    isCurrentMoveValid: boolean | null = null;
}