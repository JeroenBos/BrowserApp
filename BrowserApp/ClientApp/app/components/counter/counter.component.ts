import { Component, Input } from '@angular/core';
import { ChangesPropagator } from '../changesPropagator/ChangesPropagator';
import { BaseComponent, BaseViewModel } from '../base.component';
import { CommandInstruction } from '../../commands/commands';

@Component({
    selector: 'counter',
    templateUrl: './counter.component.html'
})
export class CounterComponent extends BaseComponent<Counter> implements Counter {
    public get currentCount() {
        return this.viewModel.currentCount;
    }

    public incrementCounter() {
        this.server.executeCommand(new CommandInstruction(0, this.__id));
    }
}


export interface Counter extends BaseViewModel {
    currentCount: number;
}
