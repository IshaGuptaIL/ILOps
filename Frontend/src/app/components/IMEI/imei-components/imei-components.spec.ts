import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ImeiComponents } from './imei-components';

describe('ImeiComponents', () => {
  let component: ImeiComponents;
  let fixture: ComponentFixture<ImeiComponents>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ImeiComponents]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ImeiComponents);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
