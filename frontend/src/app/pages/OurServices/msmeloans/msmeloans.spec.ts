import { ComponentFixture, TestBed } from '@angular/core/testing';

import { MSMELoans } from './msmeloans';

describe('MSMELoans', () => {
  let component: MSMELoans;
  let fixture: ComponentFixture<MSMELoans>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [MSMELoans],
    }).compileComponents();

    fixture = TestBed.createComponent(MSMELoans);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
