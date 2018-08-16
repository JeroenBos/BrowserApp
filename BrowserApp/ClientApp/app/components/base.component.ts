import { Component, Input, Host, OnDestroy, OnInit } from '@angular/core';
import { ChangesPropagator } from './changesPropagator/ChangesPropagator';

export abstract class BaseComponent<TViewModel extends BaseViewModel> implements OnInit {
    // @ts-ignore: server has no initializer
    @Input() public readonly viewModel: TViewModel;
    // @ts-ignore: server has no initializer
    @Input() public readonly server: ChangesPropagator;

    public get __id(): number { return this.viewModel.__id; }

    public constructor(changesPropagator?: ChangesPropagator, viewModel?: TViewModel) {
        this.server = <any>changesPropagator;
        if (viewModel !== undefined)
            this.viewModel = viewModel;
    }

    public ngOnInit() {
        if (this.server == null) {
            throw new Error(`no changes propagator was specified for component '${Object.getPrototypeOf(this).constructor.name}'`);
        }
        if (this.viewModel == null) {
            throw new Error(`Forgot to bind viewModel property on component of type '${Object.getPrototypeOf(this).constructor.name}'`);
        }
    }
}
export interface BaseViewModel {
    __id: number;
}
