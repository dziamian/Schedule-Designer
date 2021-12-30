import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ModifyScheduleComponent } from './modify-schedule.component';

describe('ModifyScheduleComponent', () => {
  let component: ModifyScheduleComponent;
  let fixture: ComponentFixture<ModifyScheduleComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ ModifyScheduleComponent ]
    })
    .compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(ModifyScheduleComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
