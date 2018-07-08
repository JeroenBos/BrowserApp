import { Component, Input } from '@angular/core';
import { ChangesPropagator } from '../changesPropagator/ChangesPropagator';

@Component({
    selector: 'counter',
    templateUrl: './counter.component.html'
})
export class CounterComponent {
    public currentCount = 0;

    @Input() server: ChangesPropagator;

    public incrementCounter() {
        this.currentCount++;
        this.server.executeCommand(0);
    }
}
