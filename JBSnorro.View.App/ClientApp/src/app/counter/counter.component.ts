import { Component } from '@angular/core';
import { BaseComponent, BaseViewModel } from './../../view.index';

@Component({
    selector: 'app-counter',
    templateUrl: './counter.component.html'
})
export class CounterComponent extends BaseComponent<Counter> implements Counter {
    public get currentCount() {
        return this.viewModel.currentCount;
    }

    public incrementCounter(e: MouseEvent) {
        this.commandManager.executeCommandByName('increment', this, e);
    }
}


export interface Counter extends BaseViewModel {
    currentCount: number;
}
