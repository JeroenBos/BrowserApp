import { Inject, ComponentFactoryResolver, ComponentFactory } from '@angular/core';
import { Http } from '@angular/http';
import { forEach } from '@angular/router/src/utils/collection';
import 'rxjs/add/operator/toPromise';
import { ChangesPropagator } from '../components/changesPropagator/ChangesPropagator';
import { BaseViewModel, BaseComponent } from '../components/base.component';

export class Command {
    public constructor(
        public readonly commandId: number,
        public readonly viewModelTypeIds: number[]) {
    }
}

export class CommandManager implements BaseViewModel {
    public readonly __id: number;
    public constructor(id: number) {
        this.__id = id;
    }
}
