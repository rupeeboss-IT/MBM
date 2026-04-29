import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ServicesDetails } from './services-details';

describe('ServicesDetails', () => {
  let component: ServicesDetails;
  let fixture: ComponentFixture<ServicesDetails>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ServicesDetails],
    }).compileComponents();

    fixture = TestBed.createComponent(ServicesDetails);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
