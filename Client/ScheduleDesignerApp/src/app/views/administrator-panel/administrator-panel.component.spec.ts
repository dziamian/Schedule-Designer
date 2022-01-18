import { ComponentFixture, TestBed } from '@angular/core/testing';

import { AdministratorPanelComponent } from './administrator-panel.component';

describe('AdministratorPanelComponent', () => {
  let component: AdministratorPanelComponent;
  let fixture: ComponentFixture<AdministratorPanelComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ AdministratorPanelComponent ]
    })
    .compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(AdministratorPanelComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
