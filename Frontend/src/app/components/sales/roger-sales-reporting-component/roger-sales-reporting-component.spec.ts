import { ComponentFixture, TestBed } from '@angular/core/testing';

import { RogerSalesReportingComponent } from './roger-sales-reporting-component';

describe('RogerSalesReportingComponent', () => {
  let component: RogerSalesReportingComponent;
  let fixture: ComponentFixture<RogerSalesReportingComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [RogerSalesReportingComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(RogerSalesReportingComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
