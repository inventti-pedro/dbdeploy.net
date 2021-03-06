using System.Collections.Generic;
using System.Linq;
using Net.Sf.Dbdeploy.Database;

namespace Net.Sf.Dbdeploy.Scripts
{
    public class RepositorioScripts : IRepositorioScripts
    {
        private readonly IDatabaseSchemaVersionManager databaseSchemaVersionManager;
        private readonly IAvailableChangeScriptsProvider changeScriptRepository;
        
        public RepositorioScripts(IDatabaseSchemaVersionManager databaseSchemaVersionManager, IAvailableChangeScriptsProvider changeScriptRepository)
        {
            this.databaseSchemaVersionManager = databaseSchemaVersionManager;
            this.changeScriptRepository = changeScriptRepository;
        }

        /// <summary>
        /// Compara os scritps aplicados e os scripts que se encontram no diret�rio e retorna uma lista dos scripts pendentes de execu��o
        /// </summary>
        /// <param name="lastChangeToApply"></param>
        /// <returns></returns>
        public IList<ChangeScript> ObterScriptsPendenteExecucao(UniqueChange lastChangeToApply)
        {
            var scriptsAplicados = ObterScriptsAplicados();
            var todosScripts = ObterTodosOsScripts();
            return IdentificarScriptsQueFaltamExecutar(lastChangeToApply, todosScripts, scriptsAplicados);
        }

        /// <summary>
        /// Obt�m todos os scrits do diret�rio
        /// </summary>
        /// <returns></returns>
        public ICollection<ChangeScript> ObterTodosOsScripts()
        {
            return changeScriptRepository.GetAvailableChangeScripts();
        }

        /// <summary>
        /// Obt�m somente os scripts aplicados
        /// </summary>
        /// <returns></returns>
        public IList<ChangeEntry> ObterScriptsAplicados()
        {
            return databaseSchemaVersionManager.GetAppliedChanges();
        }

        /// <summary>
        /// Obt�m os scripts aplicados que se encontram com status de erro na ChangeLog
        /// </summary>
        /// <returns></returns>
        public IList<ChangeEntry> ObterScritpsExecutadosComErro()
        {
            var scriptsAplicados = ObterScriptsAplicados();
            return scriptsAplicados.Where(x => x.Status == ScriptStatus.Failure).ToList();
        }

        /// <summary>
        /// Retornar um objeto ChangeEntry com as informa��es de um determinado script aplicado
        /// </summary>
        /// <param name="changeScript"></param>
        /// <returns></returns>
        public ChangeEntry ObterScriptExecutado(ChangeScript changeScript)
        {
            var scriptsAplicados = ObterScriptsAplicados();
            return scriptsAplicados.FirstOrDefault(x => x.CompareTo(changeScript) == 0);
        }

        private IList<ChangeScript> IdentificarScriptsQueFaltamExecutar(UniqueChange lastChangeToApply, IEnumerable<ChangeScript> scripts, IList<ChangeEntry> aplicados)
        {
            var listaScriptsParaAplicar = new List<ChangeScript>();

            // Re-run any scripts that have not been run, or are failed or resolved.
            // The check to exit on previous failure is done before this call.
            foreach (var script in scripts)
            {
                // If script has not been run yet, add it to the list.
                bool applyScript = false;
                var changeEntry = aplicados.FirstOrDefault(a => a.CompareTo(script) == 0);
                if (changeEntry == null)
                {
                    applyScript = true;
                }
                else
                {
                    // If the script has already been run check if it should be run again.
                    if (!changeEntry.ExecutedSuccessfully)
                    {
                        // Assign the ID so the record can be updated.
                        script.ChangeId = changeEntry.ChangeId;
                        applyScript = true;
                    }
                }

                if (applyScript)
                {
                    // Just add script if there is no cap specified.
                    if (lastChangeToApply == null)
                    {
                        listaScriptsParaAplicar.Add(script);
                    }
                    else if (script.CompareTo(lastChangeToApply) <= 0)
                    {
                        // Script is less than last change to apply.
                        listaScriptsParaAplicar.Add(script);
                    }
                    else
                    {
                        // Stop adding scripts as last change to apply has been met.
                        break;
                    }
                }
            }
            return listaScriptsParaAplicar;
        }
    }
}