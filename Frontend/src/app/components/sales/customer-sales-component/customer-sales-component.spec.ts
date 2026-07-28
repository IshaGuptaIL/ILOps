import { ComponentFixture, TestBed } from '@angular/core/testing';

import { CustomerSalesComponent } from './customer-sales-component';

describe('CustomerSalesComponent', () => {
  let component: CustomerSalesComponent;
  let fixture: ComponentFixture<CustomerSalesComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CustomerSalesComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(CustomerSalesComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
