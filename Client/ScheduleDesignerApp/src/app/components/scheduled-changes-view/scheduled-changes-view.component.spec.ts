import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ScheduledChangesViewComponent } from './scheduled-changes-view.component';

describe('ScheduledChangesViewComponent', () => {
  let component: ScheduledChangesViewComponent;
  let fixture: ComponentFixture<ScheduledChangesViewComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ ScheduledChangesViewComponent ]
    })
    .compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(ScheduledChangesViewComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
