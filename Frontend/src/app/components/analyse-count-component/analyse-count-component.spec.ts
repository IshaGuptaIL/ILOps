import { ComponentFixture, TestBed } from '@angular/core/testing';

import { AnalyseCountComponent } from './analyse-count-component';

describe('AnalyseCountComponent', () => {
  let component: AnalyseCountComponent;
  let fixture: ComponentFixture<AnalyseCountComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AnalyseCountComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(AnalyseCountComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
