import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';

export interface ComputerLocation { id: string; x: number; y: number; width?: number; height?: number; kind?: 'desk' | 'computer'; computerCount?: number; isTeacherDesk?: boolean; }
export interface ComputerSlot { offset: number; width: number; }
export function layoutComputers(desk: ComputerLocation, tableWidth: number): ComputerSlot[] {
  const count = Math.max(0, Math.trunc(desk.computerCount ?? (desk.kind === 'computer' ? 1 : 0)));
  if (!count || tableWidth <= 0) return [];
  const computerWidth = Math.min(.42, tableWidth / (count * 1.5));
  const gap = (tableWidth - count * computerWidth) / (count + 1);
  return Array.from({length: count}, (_, index) => ({
    offset: -tableWidth / 2 + gap * (index + 1) + computerWidth * (index + .5),
    width: computerWidth
  }));
}
export function computerLabel(room: RoomLayout, deskIndex: number, slotIndex: number): string {
  const previousCount = room.computers.slice(0, deskIndex).reduce((total, desk) =>
    total + Math.max(0, Math.trunc(desk.computerCount ?? (desk.kind === 'computer' ? 1 : 0))), 0);
  return `plab${previousCount + slotIndex + 1}`;
}
export interface BoardLocation { id: string; x: number; y: number; width: number; height: number; }
export interface SensorLocation extends ComputerLocation { model: string; referenceRssi: number; pathLossExponent: number; }
export interface RoomLayout {
  id: string; name: string; description: string; width: number; height: number;
  computers: ComputerLocation[]; sensors: SensorLocation[]; revision: number; boards?: BoardLocation[];
}
export function defaultBoard(room: Pick<RoomLayout, 'width' | 'height'>): BoardLocation {
  const height = Math.min(.12, room.height * .1);
  return { id: 'TABLA-1', x: room.width / 2, y: height / 2, width: room.width * .5, height };
}
export function roomBoards(room: RoomLayout): BoardLocation[] {
  return room.boards?.length ? room.boards : [defaultBoard(room)];
}
export const EMPTY_ROOM: RoomLayout = {
  id: '', name: 'Učionica', description: '', width: 8, height: 6,
  computers: [], sensors: [], boards: [], revision: 1
};
@Injectable({providedIn: 'root'})
export class Rooms {
  private readonly http = inject(HttpClient);
  readonly items = signal<RoomLayout[]>([]);
  readonly error = signal('');
  load(): void { this.http.get<RoomLayout[]>('/api/classrooms').subscribe({next: rooms => { this.items.set(rooms); this.error.set(''); }, error: () => this.error.set('Učionice nije moguće učitati.')}); }
  save(room: RoomLayout, existing: boolean) {
    return existing ? this.http.put<RoomLayout>(`/api/classrooms/${encodeURIComponent(room.id)}`,room) : this.http.post<RoomLayout>('/api/classrooms',room);
  }
}
