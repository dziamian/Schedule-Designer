import { ComponentFixture, TestBed } from '@angular/core/testing';

import { AvailableResourcesComponent } from './available-resources.component';

describe('AvailableResourcesComponent', () => {
  let component: AvailableResourcesComponent;
  let fixture: ComponentFixture<AvailableResourcesComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ AvailableResourcesComponent ]
    })
    .compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(AvailableResourcesComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
