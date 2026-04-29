import { ComponentFixture, TestBed } from '@angular/core/testing';

import { Coaching } from './coaching';

describe('Coaching', () => {
  let component: Coaching;
  let fixture: ComponentFixture<Coaching>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Coaching],
    }).compileComponents();

    fixture = TestBed.createComponent(Coaching);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
