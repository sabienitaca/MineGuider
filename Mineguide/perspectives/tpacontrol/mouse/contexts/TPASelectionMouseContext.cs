using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Castle.MicroKernel.Registration;
using pm4h.tpa;
using pm4h.windows.ui.fragments.tpaviewer;
using pm4h.windows.ui.fragments.tpaviewer.designer.tpacontrol.mouse.contexts;
using pm4h.windows.ui.fragments.tpaviewer.designer.tpacontrol.visualelements;

namespace Mineguide.perspectives.tpacontrol.mouse.contexts
{
    public class TPASelectionMouseContext : TPAViewMouseContext
    {
        public enum ContextState { Default, ClickSelection };

        public ContextState State { get; set; } = ContextState.Default;

        public TPASelectionMouseContext(TPAViewerEngine eng) : base(eng)
        {

        }

        //public List<Estado> SelectedStates { get; set; } = new List<Estado>();
        public List<Estado> GetSelectedStates()
        {
            List<Estado> result = new List<Estado>();
            foreach (var n in this.TPAviewer.nodos.Values)
            {
                if (n.IsSelected())
                {
                    result.Add(n);
                }
            }
            return result;
        }

        Estado _lastSelectedState = null;

        public override void Handle_MouseSingleDown(object sender, MouseButtonEventArgs args)
        {
            // comportamiento por defecto para los estados iniciales y finales
            if (sender is EstadoInicial || sender is EstadoFinal)
            {
                base.Handle_MouseSingleDown(sender, args);
                return;
            }



            if (sender is Estado state)
            {
                _lastSelectedState = state;

                switch (State)
                {
                    case ContextState.ClickSelection:
                        if (state.IsBlocked()) // no se completara el click si esta bloqueado
                        {
                            _lastSelectedState = null; // no se completara el click si esta bloqueado
                        }
                        // else --> No hacemos nada pq vamos a añadir el elemento en el MouseUp
                        break;
                    case ContextState.Default:
                    default:
                        base.Handle_MouseSingleDown(sender, args);

                        // Guardo INFO para el IF posterior
                        bool moreStatesSelected = !GetSelectedStates().Any(s => s != state); // me guardo si hay mas estados seleccionados que no sean este que ha capturado el click
                        bool selectedState = state.IsSelected();// me guardo si el estado esta seleccionado

                        ClearSelection(); // deselecciono todos los estados

                        // Usando INFO de si había mas estados seleccionados que no sean este, lo selecciono para que el MouseUp lo deseleccione
                        if (selectedState && moreStatesSelected) { state.Select(); }
                        break;
                }
            }
            else
            {
                base.Handle_MouseSingleDown(sender, args);
                ClearSelection();
            }
        }

        public void ClearSelection()
        {
            foreach (var state in GetSelectedStates())
            {
                state.Deselect();
            }
            //SelectedStates.Clear();
        }

        public override void Handle_MouseUp(object sender, MouseButtonEventArgs args)
        {
            // comportamiento por defecto para los estados iniciales y finales
            if (sender is EstadoInicial || sender is EstadoFinal)
            {
                base.Handle_MouseUp(sender, args);
                return;
            }

            // Si el elemento en up era el mismo de down se completa el click
            if (_lastSelectedState != null && sender is Estado state && state == _lastSelectedState)
            {
                switch (State)
                {
                    case ContextState.ClickSelection:
                    case ContextState.Default:
                    default:
                        base.Handle_MouseUp(sender, args);
                        if (!state.IsSelected()) //seleccionamos el elemento
                        {
                            state.Select();
                            //SelectedStates.Add(state);
                        }
                        else // deseleccionamos el elemento
                        {
                            state.Deselect();
                            //SelectedStates.Remove(state);
                        }
                        break;
                }
            }
            else
            {
                base.Handle_MouseUp(sender, args);
            }
        }
    }
}
