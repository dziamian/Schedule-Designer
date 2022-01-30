import { ComponentFixture, TestBed } from '@angular/core/testing';

import { RoomTypeFieldComponent } from './room-type-field.component';

describe('RoomTypeFieldComponent', () => {
  let component: RoomTypeFieldComponent;
  let fixture: ComponentFixture<RoomTypeFieldComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ RoomTypeFieldComponent ]
    })
    .compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(RoomTypeFieldComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
