import { CdkDrag, CdkDragRelease, CdkDragStart } from '@angular/cdk/drag-drop';
import { Component, Input, OnInit, Output, EventEmitter, ViewChild, SimpleChanges } from '@angular/core';
import { Store } from '@ngrx/store';
import { UserInfo } from 'src/app/others/Accounts';
import { CourseEdition } from 'src/app/others/CourseEdition';
import { Settings } from 'src/app/others/Settings';

/**
 * Komponent pojedynczego panelu z zajęciami.
 */
@Component({
  selector: 'app-course',
  templateUrl: './course.component.html',
  styleUrls: ['./course.component.css']
})
export class CourseComponent implements OnInit {

  /** Reprezentacja elementu panelu (przechowującego informacje o edycji zajęć), który może być przeciągany po ekranie. */
  @ViewChild(CdkDrag) cdkCourse : CdkDrag<CourseEdition>;

  /** Określa czy tryb modyfikacji jest włączony. */
  @Input() isModifying:boolean;
  /** Określa czy blokowania zwykłych użytkowników są ignorowane. */
  @Input() ignoreUsersLocks:boolean;
  /** Identyfikatory grup studenckich, których student jest starostą. */
  @Input() representativeGroupsIds:number[] = [];
  /** Informacje o edycji zajęć, których panel dotyczy. */
  @Input() course:CourseEdition;
  /** Ustawienia aplikacji. */
  @Input() settings:Settings;
  /** Tygodnie dotyczące aktualnego widoku. */
  @Input() weeksOnTab:number[];
  /** Określa czy aktualnie jest wykonywane jakieś 
   * przesunięcie w planie przez użytkownika (nie dotyczy interakcji przeciągania i upuszczania). 
   */
  @Input() isSelectedMoving:boolean|undefined;
  
  /** 
   * Emiter zdarzenia wybrania panelu z zajęciami (wysyłany w momencie kliknięcia na panel).
   * Zdarzenie posiada informacje o edycji wybranych zajęć, czy są możliwe do przesuwania w obecnym widoku
   * oraz ich pozycji w planie (rama czasowa).
   */
  @Output() onSelect:EventEmitter<{courseEdition:CourseEdition,isDisabled:boolean}> = new EventEmitter<{courseEdition:CourseEdition,isDisabled:boolean}>();
  /** Emiter zdarzenia rozpoczęcia interakcji przeciągania panelu z zajęciami. */
  @Output() onStart:EventEmitter<CdkDragStart> = new EventEmitter<CdkDragStart>();
  /** Emiter zdarzenia zakończenia interakcji przeciągania panelu z zajęciami. */
  @Output() onRelease:EventEmitter<CdkDragRelease> = new EventEmitter<CdkDragRelease>();
  
  /** Informacje o zalogowanym użytkowniku. */
  userInfo:UserInfo;

  constructor(
    private store:Store<{userInfo:UserInfo}>
  ) {
    this.store.select('userInfo').subscribe((userInfo) => {
      if (userInfo.UserId == 0) {
        return;
      }
      this.userInfo = userInfo;
    });
  }

  getScheduledMovesBadge(): number  {
    return this.course.getScheduledMovesBadge(this.isModifying);
  }

  /**
   * Metoda zwracająca styl tła panelu w zależności od aktualnego stanu.
   * @returns Styl tła panelu
   */
  getBackground(): string {
    return this.course?.IsLocked
      ? ((!this.course?.IsLockedByAdmin)
        ? `repeating-linear-gradient(90deg, ${this.course?.Type?.Color}, ${this.course?.Type?.Color} 10px, #FFFFFF 10px, #FFFFFF 20px) left / 50% 100% no-repeat,
        ${this.course?.Type?.Color} right / 50% 100% no-repeat`
        : `repeating-linear-gradient(90deg, ${this.course?.Type?.Color}, ${this.course?.Type?.Color} 10px, #FFFFFF 10px, #FFFFFF 20px)`)
      : `${this.course?.Type?.Color}`;
  }

  ngOnInit(): void {
  }

  Click(event:MouseEvent) {
    this.onSelect.emit({courseEdition: this.course, isDisabled: this.cdkCourse.disabled});
  }

  /**
   * Funkcja sprawdzająca czy powinno być uniemożliwione zmienianie pozycji edycji zajęć na planie
   * biorąc pod uwagę aktualny widok.
   * @returns Prawdę, jeśli tygodnie aktualnego widoku nie zgadzają się z tygodniami pozycji w planie,
   * w przeciwnym wypadku fałsz.
   */
  CheckIfNotMatching():boolean {
    if (this.course.Weeks == null) return false;
    return this.weeksOnTab?.sort((a,b) => a - b).join(',') 
      !== this.course.Weeks?.sort((a,b) => a - b).join(',');
  }

  /**
   * Funkcja sprawdzająca czy powinno być możliwe zmienianie pozycji edycji zajęć na planie
   * biorąc pod uwagę aktualny widok.
   * @returns Prawdę, jeśli tygodnie aktualnego widoku zgadzają się z tygodniami pozycji w planie,
   * w przeciwnym wypadku fałsz.
   */
  CheckIfMatching():boolean {
    if (this.course.Weeks == null) return false;
    return this.weeksOnTab?.sort((a,b) => a - b).join(',') 
      === this.course.Weeks?.sort((a,b) => a - b).join(',');
  }

  /**
   * Funkcja sprawdzająca czy zaplanowane ruchy przypisane do edycji zajęć
   * posiadają jakieś propozycje.
   * @returns Prawdę, jeśli posiadają, w przeciwnym wypadku fałsz.
   */
  CheckIfAnyProposition():boolean {
    if (!this.isModifying) {
      return false;
    }
    return this.course.ScheduledMoves.some((scheduledMove) => !scheduledMove.IsConfirmed);
  }

  CheckIfInvalidGroup(): boolean {
    if (this.representativeGroupsIds.length == 0) {
      return false;
    }
    return !this.course.Groups.some(group => this.representativeGroupsIds.includes(group.GroupId));
  }

  OnStarted(event:CdkDragStart) {
    this.onStart.emit(event);
  }

  OnReleased(event:CdkDragRelease) {
    this.onRelease.emit(event);
  }

  Floor(number:number):number {
    return Math.floor(number);
  }

  Ceil(number:number):number {
    return Math.ceil(number);
  }
}
