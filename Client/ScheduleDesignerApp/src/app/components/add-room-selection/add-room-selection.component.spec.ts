import { ComponentFixture, TestBed } from '@angular/core/testing';

import { AddRoomSelectionComponent } from './add-room-selection.component';

describe('AddRoomSelectionComponent', () => {
  let component: AddRoomSelectionComponent;
  let fixture: ComponentFixture<AddRoomSelectionComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ AddRoomSelectionComponent ]
    })
    .compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(AddRoomSelectionComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
