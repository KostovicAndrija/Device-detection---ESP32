import { ComponentFixture, TestBed } from '@angular/core/testing';
import { RoomLayout } from '../../core/services/rooms';
import { RoomMapComponent } from './room-map';

describe('RoomMapComponent', () => {
  let fixture: ComponentFixture<RoomMapComponent>;
  const room: RoomLayout = {
    id: 'LAB', name: 'Laboratorija', description: '', width: 8, height: 6, revision: 1,
    sensors: [{id: 'S1', x: 1, y: 1, model: 'ESP32', referenceRssi: -45, pathLossExponent: 2.7}],
    computers: [
      {id: 'K-1', x: 2, y: 2, width: 2, height: .6, kind: 'desk', computerCount: 2},
      {id: 'K-2', x: 5, y: 2, width: 2, height: .6, kind: 'desk', computerCount: 2}
    ]
  };

  beforeEach(async () => {
    await TestBed.configureTestingModule({imports: [RoomMapComponent]}).compileComponents();
    fixture = TestBed.createComponent(RoomMapComponent);
    fixture.componentRef.setInput('room', room);
    fixture.detectChanges();
  });

  it('renders one shared map with sequential plab labels', () => {
    const nodes = fixture.nativeElement.querySelectorAll('.computer-label') as NodeListOf<Element>;
    const labels = Array.from(nodes).map(node => node.textContent?.trim());
    expect(labels).toEqual(['plab1', 'plab2', 'plab3', 'plab4']);
  });

  it('emits the selected desk only in editable mode', () => {
    const selected = jasmine.createSpy('selected');
    fixture.componentInstance.deskSelected.subscribe(selected);
    (fixture.nativeElement.querySelector('.desk-shape') as SVGGElement).dispatchEvent(new MouseEvent('click', {bubbles: true}));
    expect(selected).not.toHaveBeenCalled();
    fixture.componentRef.setInput('editable', true); fixture.detectChanges();
    (fixture.nativeElement.querySelector('.desk-shape') as SVGGElement).dispatchEvent(new MouseEvent('click', {bubbles: true}));
    expect(selected).toHaveBeenCalledOnceWith(0);
  });
});
