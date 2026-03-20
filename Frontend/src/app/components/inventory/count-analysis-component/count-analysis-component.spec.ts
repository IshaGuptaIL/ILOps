import { ComponentFixture, TestBed } from '@angular/core/testing';

import { CountAnalysisComponent } from './count-analysis-component';

describe('CountAnalysisComponent', () => {
  let component: CountAnalysisComponent;
  let fixture: ComponentFixture<CountAnalysisComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CountAnalysisComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(CountAnalysisComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
