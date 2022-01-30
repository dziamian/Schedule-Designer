import { ComponentFixture, TestBed } from '@angular/core/testing';

import { RoomFieldComponent } from './room-field.component';

describe('RoomFieldComponent', () => {
  let component: RoomFieldComponent;
  let fixture: ComponentFixture<RoomFieldComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ RoomFieldComponent ]
    })
    .compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(RoomFieldComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
