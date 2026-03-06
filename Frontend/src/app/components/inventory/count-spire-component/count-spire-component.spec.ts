import { ComponentFixture, TestBed } from '@angular/core/testing';

import { CountSpireComponent } from './count-spire-component';

describe('CountSpireComponent', () => {
  let component: CountSpireComponent;
  let fixture: ComponentFixture<CountSpireComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CountSpireComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(CountSpireComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
