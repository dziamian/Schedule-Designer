import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ExportFieldComponent } from './export-field.component';

describe('ExportFieldComponent', () => {
  let component: ExportFieldComponent;
  let fixture: ComponentFixture<ExportFieldComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ ExportFieldComponent ]
    })
    .compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(ExportFieldComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
