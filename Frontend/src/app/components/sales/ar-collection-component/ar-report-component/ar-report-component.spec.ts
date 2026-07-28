import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ArReportComponent } from './ar-report-component';

describe('ArReportComponent', () => {
  let component: ArReportComponent;
  let fixture: ComponentFixture<ArReportComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ArReportComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ArReportComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
