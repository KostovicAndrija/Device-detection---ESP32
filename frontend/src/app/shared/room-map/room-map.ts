import { DecimalPipe } from '@angular/common';
import { Component, EventEmitter, Input, Output } from '@angular/core';
import { DevicePosition } from '../../core/services/api';
import { BoardLocation, computerLabel, ComputerLocation, ComputerSlot, layoutComputers, RoomLayout, roomBoards } from '../../core/services/rooms';

@Component({
  selector: 'app-room-map',
  imports: [DecimalPipe],
  templateUrl: './room-map.html',
  styleUrl: './room-map.scss'
})
export class RoomMapComponent {
  @Input({required: true}) room!: RoomLayout;
  @Input() positions: DevicePosition[] = [];
  @Input() viewTime = Date.now();
  @Input() editable = false;
  @Input() fadeDevices = false;
  @Input() compact = false;
  @Input() expanded = false;
  @Output() deskSelected = new EventEmitter<number>();
  @Output() sensorSelected = new EventEmitter<number>();

  boards(): BoardLocation[] { return roomBoards(this.room); }
  deskWidth(desk: ComputerLocation): number { return desk.width ?? this.room.width * .13; }
  deskHeight(desk: ComputerLocation): number { return desk.height ?? this.room.height * .08; }
  fontSize(): number { return Math.min(this.room.width, this.room.height) * .025; }
  sensorX(x: number): number { return Math.max(this.fontSize() * 2, Math.min(this.room.width - this.fontSize() * 2, x)); }
  sensorY(y: number): number { return Math.max(this.fontSize() * 1.4, Math.min(this.room.height - this.fontSize() * 1.4, y)); }
  computerSlots(desk: ComputerLocation): ComputerSlot[] { return layoutComputers(desk, this.deskWidth(desk)); }
  computerLabel(deskIndex: number, slotIndex: number): string { return computerLabel(this.room, deskIndex, slotIndex); }
  shortId(id: string): string { return id.slice(0, 4).toUpperCase(); }
  signalLabel(type?: string): string { return type === 'wifi' ? 'Wi-Fi' : type === 'bluetooth' ? 'Bluetooth' : type === 'mixed' ? 'Wi-Fi/BT' : 'Nepoznato'; }
  signalIcon(type?: string): string { return type === 'wifi' ? 'wifi' : type === 'bluetooth' ? 'bluetooth' : type === 'mixed' ? 'settings_input_antenna' : 'device_unknown'; }
  opacity(position: DevicePosition): number {
    if (!this.fadeDevices) return 1;
    const age = this.ageMs(position);
    return age <= 8_000 ? 1 : Math.max(0, 1 - (age - 8_000) / 37_000);
  }
  presence(position: DevicePosition): 'live' | 'fading' { return this.ageMs(position) < 8_000 ? 'live' : 'fading'; }
  selectDesk(index: number): void { if (this.editable) this.deskSelected.emit(index); }
  selectSensor(index: number): void { if (this.editable) this.sensorSelected.emit(index); }
  private ageMs(position: DevicePosition): number { return Math.max(0, this.viewTime - Date.parse(position.capturedAt)); }
}
