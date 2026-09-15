import { Component, OnInit, OnDestroy, inject } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from '../../core/auth/auth.service';
import { Realtime } from '../../core/services/realtime';

@Component({
  selector: 'app-main-layout',
  imports: [RouterLink, RouterLinkActive, RouterOutlet],
  templateUrl: './main-layout.html',
  styleUrl: './main-layout.scss',
})
export class MainLayout implements OnInit, OnDestroy {
  readonly auth = inject(AuthService);
  readonly realtime = inject(Realtime);
  readonly connectionLabels = {
    connecting: 'Povezivanje…',
    connected: 'Povezano',
    disconnected: 'Veza je prekinuta'
  };

  ngOnInit(): void {
    void this.realtime.connect();
  }
  ngOnDestroy(): void { void this.realtime.disconnect(); }
}
