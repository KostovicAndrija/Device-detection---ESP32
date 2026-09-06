import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Api, Device } from '../../../../core/services/api';

@Component({ selector: 'app-devices', imports: [FormsModule, DatePipe], templateUrl: './devices.html', styleUrl: './devices.scss' })
export class Devices implements OnInit {
  private readonly api = inject(Api);
  readonly devices = signal<Device[]>([]);
  readonly query = signal('');
  readonly filtered = computed(() => {
    const value = this.query().trim().toLowerCase();
    return value ? this.devices().filter(x => x.hashId.includes(value) || x.type.toLowerCase().includes(value)) : this.devices();
  });
  windowMinutes = 10;
  ngOnInit(): void { this.load(); }
  load(): void { this.api.activeDevices(this.windowMinutes).subscribe(x => this.devices.set(x)); }
}
