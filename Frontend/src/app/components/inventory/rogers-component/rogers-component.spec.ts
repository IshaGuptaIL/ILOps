import { ComponentFixture, TestBed } from '@angular/core/testing';

import { RogersComponent } from './rogers-component';

describe('RogersComponent', () => {
  let component: RogersComponent;
  let fixture: ComponentFixture<RogersComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [RogersComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(RogersComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
