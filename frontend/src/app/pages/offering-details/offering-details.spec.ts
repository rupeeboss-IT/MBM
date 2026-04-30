import { ComponentFixture, TestBed } from '@angular/core/testing';

import { OfferingDetails } from './offering-details';

describe('OfferingDetails', () => {
  let component: OfferingDetails;
  let fixture: ComponentFixture<OfferingDetails>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [OfferingDetails],
    }).compileComponents();

    fixture = TestBed.createComponent(OfferingDetails);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
