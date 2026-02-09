import { ComponentFixture, TestBed } from '@angular/core/testing';

import { CostValidationComponent } from './cost-validation-component';

describe('CostValidationComponent', () => {
  let component: CostValidationComponent;
  let fixture: ComponentFixture<CostValidationComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CostValidationComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(CostValidationComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
